# mechanics-execution-service

Execution Service — one of the three microservices of the [Mechanics Software](https://github.com/Torque-OS/mechanics-software) Fase 4 architecture (FIAP POS Tech 15SOAT).

## Responsibility

Per [ADR-010](https://github.com/Torque-OS/mechanics-software/blob/main/docs/decisions/ADR-010-microservices-split.md):

- Manage the execution queue for service orders once payment is confirmed.
- Track diagnosis and repair status.
- React to the `EnqueueExecution` command and publish `ExecutionCompleted`/`ExecutionFailed` events to the OS
  Service's Saga orchestrator ([ADR-011](https://github.com/Torque-OS/mechanics-software/blob/main/docs/decisions/ADR-011-saga-orquestrada-e-mensageria.md)).

This repo owns its own data — **no other service may query this database directly**
([ADR-012](https://github.com/Torque-OS/mechanics-software/blob/main/docs/decisions/ADR-012-banco-por-servico.md)): PostgreSQL (`execution_orders`, system of record) and MongoDB (`execution_queue`, a read-optimized
projection of the queue — see [Persistence](#persistence)).

## Status

- **Scaffold (F4-06): done.**
- **Execution queue (F4-18): done.** `EnqueueExecution` saga command in, queue entry out — see
  [Saga messages](#saga-messages). In the cluster the broker comes from `mechanics-infra-k8s` (F4-08,
  see [Broker in the cluster](#broker-in-the-cluster)); local development uses this repo's own `docker compose` RabbitMQ.
- **Diagnosis/repair status (F4-19): done.** `POST /api/executions/{id}/start-diagnosis` moves
  the order to `IN_PROGRESS` and off the queue; `POST/GET /api/executions/{id}/notes` record and
  list diagnosis/repair notes in MongoDB.
- **Completion/failure (F4-20): done.** `POST /api/executions/{id}/complete` and `.../fail`
  publish `ExecutionCompleted`/`ExecutionFailed` — see [Saga messages](#saga-messages).
- **Compensation (F4-21): done.** Consumes `CancelExecution` from the OS Service and cancels the
  order, wherever it is in the queue or in progress.
- **Messaging reliability (F4-24): done.** Inbox, outbox and the shared retry policy on every consumer.
- The OS Service sends `EnqueueExecution` and consumes `ExecutionCompleted`/`ExecutionFailed` (F4-12, F4-22).
  It does not send `CancelExecution` yet: the current compensations happen before an order is enqueued or after
  the execution itself failed. See the [Fase 4 board](https://github.com/orgs/Torque-OS/projects/4).
- **Authentication and API Gateway (F4-13): done.** Every `/api/executions` route requires a platform JWT with a staff
  role (`ADMIN`, `ATTENDANT` or `MECHANIC`), validated with the same `JWT_SECRET`, issuer and audience as the other
  services. The Service is a `NodePort` (30082) that the API Gateway in `mechanics-infra-k8s` routes `/api/executions`
  to — see [Authentication](#authentication).
- **Distributed tracing (F4-36): done.** See [Observability](#observability).

## Stack

C# 12 · ASP.NET Core 8 · PostgreSQL 16 · MongoDB · RabbitMQ + MassTransit 8.5 (ADR-011) · xUnit + FluentAssertions

## Running locally

```bash
docker compose up --build
```

The API listens on `http://localhost:8082`, with `GET /health` for a liveness check and Swagger at `/swagger` in
Development. PostgreSQL is exposed on `5437`, MongoDB on `27019`, and this repo's own RabbitMQ on `5673` (management
UI at `http://localhost:15673`, `guest`/`guest`) — all offset from `mechanics-software`'s and
`mechanics-billing-service`'s local stacks so the three can run side by side. Migrations are applied on startup.

Without Docker for the API: `docker compose up postgres mongo rabbitmq -d`, then
`dotnet run --project src/MechanicsSoftware.ExecutionService.Api`.

## Persistence

- **PostgreSQL** (`execution_orders`): the system of record. One row per service order
  (`service_order_id` is unique — also how `EnqueueExecutionHandler` stays idempotent), plus the
  MassTransit transactional outbox/inbox tables.
- **MongoDB**:
  - `execution_queue` — a denormalized, priority-ordered view written alongside the Postgres row.
    Reads for "what's next" don't compete with the transactional writes. An entry is removed when
    diagnosis starts (F4-19) or a compensation cancels the order (F4-21).
  - `execution_notes` — append-only diagnosis/repair notes (F4-19), one document per note.

## Endpoints

| Method | Path | |
|---|---|---|
| GET | `/api/executions` | Current queue, ordered by priority then arrival time |
| GET | `/api/executions/{id}` | A single execution order |
| POST | `/api/executions/{id}/start-diagnosis` | `QUEUED` → `IN_PROGRESS`; removes the order from the queue |
| POST | `/api/executions/{id}/notes` | Record a diagnosis/repair note (only while `IN_PROGRESS`) — body `{ "text": "..." }` |
| GET | `/api/executions/{id}/notes` | Notes for an order, oldest first |
| POST | `/api/executions/{id}/complete` | `IN_PROGRESS` → `COMPLETED`; publishes `ExecutionCompleted` |
| POST | `/api/executions/{id}/fail` | `IN_PROGRESS` → `FAILED`; publishes `ExecutionFailed` — body `{ "reason": "..." }` |
| GET | `/health` | Liveness/readiness probe |

Every route except `/health` requires `Authorization: Bearer <staff JWT>` — see [Authentication](#authentication).

## Authentication

- Tokens are issued by `mechanics-software` (`POST /api/auth/login`, staff) or by `mechanics-lambda` (`POST /auth`,
  customers by CPF). This service only validates them; it never issues one.
- `JWT_SECRET` must be the same in `mechanics-lambda`, `mechanics-software`, `mechanics-billing-service` and here.
  `JWT_ISSUER` and `JWT_AUDIENCE` default to `torque-os` and `mechanics-software-api`, like everywhere else.
- No token or an invalid one → `401`; a valid customer token → `403`; a staff token → the route runs.
- In the cluster the API Gateway (`mechanics-infra-k8s`) also checks the token with the Lambda authorizer and routes
  `/api/executions` to the `NodePort` 30082 Service. The Service keeps its cluster-internal address, so in-cluster
  callers are unaffected.
- Locally, `docker compose` sets the development secret; get a staff token from `mechanics-software`
  (`admin@mechanics.local` / `Admin@123` in Development):

```bash
TOKEN=$(curl -s -H 'Content-Type: application/json' -d '{"email":"admin@mechanics.local","password":"Admin@123"}' \
  http://localhost:8080/api/auth/login | jq -r .token)
curl -H "Authorization: Bearer $TOKEN" http://localhost:8082/api/executions
```

## Saga messages

MassTransit 8.5 over RabbitMQ, with the EF Core transactional inbox/outbox (ADR-011): a redelivery of the same
`MessageId` is dropped before a consumer runs, and a published event is stored with the state change and delivered
only once `SaveChanges` commits.

### Consumes

| Message | From | Payload |
|---|---|---|
| `EnqueueExecution` | OS Service | `{ serviceOrderId: guid, priority: int = 0 }` |
| `CancelExecution` | OS Service (compensation) | `{ serviceOrderId: guid }` |

- Both are idempotent: re-sending `EnqueueExecution` for an already-queued `serviceOrderId`, or `CancelExecution` for
  an already-cancelled one, is a no-op. `EnqueueExecution` is also backed by a unique index, not just the inbox.
- `CancelExecution` is only sent while the order may still be cancelled — never after this service has already
  published `ExecutionCompleted`.
- **Retry (F4-24), the same for every consumer** (`SagaRetryPolicy`, shared with the Billing Service): transient
  failures (database, broker) are retried 5 times with exponential backoff, about 16 s in total. Business rule
  violations and unknown orders are permanent and are not retried. A message that still fails goes to
  `execution.enqueue-execution_error` / `execution.cancel-execution_error`; move it back with the RabbitMQ
  shovel/"Move messages" once the cause is fixed.
- Local test without the OS Service — RabbitMQ UI → *Exchanges* → `TorqueOS.Contracts.Execution:EnqueueExecution` →
  *Publish message*, property `content_type` = `application/vnd.masstransit+json`, payload:

  ```json
  { "messageId": "<new guid>", "messageType": ["urn:message:TorqueOS.Contracts.Execution:EnqueueExecution"],
    "message": { "serviceOrderId": "<guid>", "priority": 0 } }
  ```

### Publishes

| Message | To | Payload |
|---|---|---|
| `ExecutionCompleted` | OS Service | `{ serviceOrderId, executionOrderId, completedAt }` |
| `ExecutionFailed` | OS Service | `{ serviceOrderId, executionOrderId, reason, failedAt }` — triggers Saga compensation |

Sent from `POST /api/executions/{id}/complete` / `.../fail` (staff-driven). The OS Service's orchestrator consumes them
(F4-12): `ExecutionCompleted` completes the service order, `ExecutionFailed` cancels it and compensates the payment.

**For the OS Service (the Saga orchestrator):**

- Declare records with exactly this namespace, name and fields — MassTransit identifies a message by
  `urn:message:TorqueOS.Contracts.Execution:<Name>`. Copy them from
  `src/MechanicsSoftware.ExecutionService.Application/Contracts/`.
- Each message has its own fanout exchange named `TorqueOS.Contracts.Execution:<Name>`; this service's consumers (for
  `EnqueueExecution` and `CancelExecution`) bind their queues to it automatically.
- `priority` is optional and currently unused by any real caller — every order queues at the same priority (plain
  FIFO by arrival) until the OS Service has a reason to set it.
- `CorrelationId` on every published message is the `ServiceOrderId`.
- Evolution: fields are only ever added. A breaking change gets a new type (e.g. `EnqueueExecutionV2`).

## Observability

OpenTelemetry traces every inbound HTTP request and every MassTransit publish/consume (ADR-011, F4-36), so a
`ServiceOrderId`'s trace continues from the OS Service's saga orchestrator into this service's `EnqueueExecution`
consumer — the message headers already carry W3C trace context automatically, courtesy of MassTransit's own
`ActivitySource`. Nothing is exported unless `OTEL_EXPORTER_OTLP_ENDPOINT` is set (empty/unset in local `docker
compose` by default — point it at a local collector such as Jaeger or the cluster's Datadog Agent to see spans); the
exporter is otherwise left out of the pipeline entirely.

- **Do not set `options.Endpoint` on the OTLP exporter in code.** The exporter only appends the per-signal path
  (`/v1/traces`) when it reads `OTEL_EXPORTER_OTLP_ENDPOINT`/`OTEL_EXPORTER_OTLP_PROTOCOL` itself; overwriting
  `Endpoint` afterwards replaces that already-suffixed URI with the bare host, and the collector 404s every export —
  silently, since OTel's own export failures aren't logged by default. Confirmed against a real Jaeger instance
  while implementing this issue (traces simply never arrived, with no error anywhere, until this was fixed).

## Broker in the cluster

`mechanics-infra-k8s` (F4-08) provisions one RabbitMQ shared by the three services, with a **single vhost,
`/torque-os`**, and one user per service. The vhost must be URL-encoded in `RABBITMQ_CONNECTION_STRING`:
`amqp://execution-service:<password>@rabbitmq.messaging.svc.cluster.local:5672/%2Ftorque-os`.

## Project layout

```
src/MechanicsSoftware.ExecutionService.Domain/         ExecutionOrder, ExecutionStatus
src/MechanicsSoftware.ExecutionService.Application/     Handlers, IExecutionDbContext, IExecutionQueue, Contracts/
src/MechanicsSoftware.ExecutionService.Infrastructure/  EF Core (Postgres), Mongo queue, MassTransit consumer
src/MechanicsSoftware.ExecutionService.Api/             Controllers, Program.cs
tests/MechanicsSoftware.ExecutionService.UnitTests/
k8s/                                                     Namespace, Deployment, Service, ConfigMap, Secret, HPA
.github/workflows/ci.yml                                 Build, test, coverage gate, SonarCloud, Docker build (F4-30)
.github/workflows/deploy.yml                             Push image to GHCR, deploy to EKS (F4-34)
```

## CI/CD

- **`ci.yml`** (F4-30) — on every push/PR to `main`: build, run unit tests with coverage, enforce
  an 80% line-coverage gate, run a SonarCloud scan, and build the Docker image. The SonarCloud
  steps are `continue-on-error`, so a missing `SONAR_TOKEN` degrades to "no quality report" rather
  than failing the whole pipeline — **requires**:
  - The `SONAR_TOKEN` repo secret.
  - A SonarCloud project with key `rnataoliveira_mechanics-execution-service` under the
    `rnataoliveira` org (auto-provisioned on first scan if using SonarCloud's GitHub-connected
    CI-based analysis; otherwise create it manually first).
- **`deploy.yml`** (F4-34) — on a successful `ci.yml` run on `main`: build and push the image to
  GHCR, then apply the `k8s/` manifests to the `mechanics-execution` namespace of the shared EKS
  cluster (the same cluster `mechanics-software` deploys to, per ADR-010 — provisioned by
  [`mechanics-infra-k8s`](https://github.com/Torque-OS/mechanics-infra-k8s), F4-08/F4-09) —
  **requires** these repo secrets: `AWS_ACCESS_KEY_ID`, `AWS_SECRET_ACCESS_KEY` (same IAM user as
  `mechanics-software`), `DB_HOST`, `DB_USER`, `DB_PASSWORD`, `MONGO_CONNECTION_STRING`,
  `RABBITMQ_CONNECTION_STRING`, `JWT_SECRET`. None of these are set yet, and the EKS cluster isn't currently
  running — `deploy.yml` will fail at the `aws eks update-kubeconfig` step until both exist. The
  pipeline is ready; the infrastructure to run it against isn't.

## Testing & Coverage Evidence (F4-28)

```bash
dotnet test --collect:"XPlat Code Coverage" --settings coverlet.runsettings

## Contributing

`main` is protected — open a PR from a branch and get one approval. Conventional commits.
