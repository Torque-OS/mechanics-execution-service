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
  [Saga messages](#saga-messages). No broker/RabbitMQ provisioning in the cluster yet (F4-08);
  local development uses this repo's own `docker compose` RabbitMQ.
- **Diagnosis/repair status (F4-19): done.** `POST /api/executions/{id}/start-diagnosis` moves
  the order to `IN_PROGRESS` and off the queue; `POST/GET /api/executions/{id}/notes` record and
  list diagnosis/repair notes in MongoDB.
- **Completion/failure (F4-20): done.** `POST /api/executions/{id}/complete` and `.../fail`
  publish `ExecutionCompleted`/`ExecutionFailed` — see [Saga messages](#saga-messages).
- **Compensation (F4-21): done.** Consumes `CancelExecution` from the OS Service and cancels the
  order, wherever it is in the queue or in progress.
- F4-18 to F4-21 (this repo's full scope) are complete. What's still missing for the real,
  end-to-end Saga is outside this repo: the broker in the cluster (F4-08) and the OS Service's
  orchestrator actually sending `EnqueueExecution`/`CancelExecution` and consuming
  `ExecutionCompleted`/`ExecutionFailed` (F4-13/F4-22). See the
  [Fase 4 board](https://github.com/orgs/Torque-OS/projects/4).
- No authentication on the API yet (unlike `mechanics-billing-service`, which requires a JWT on
  every controller) — the only endpoints today are read-only queue views. Add it alongside F4-19
  if staff-facing write actions are needed, or as its own issue.

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

No authentication yet — see [Status](#status).

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
- Database failures are retried (1 s, 5 s, 15 s). A message that still fails goes to
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

Sent from `POST /api/executions/{id}/complete` / `.../fail` today (staff-driven); once F4-13/F4-22 wire the real
Saga, these are the events the OS Service's orchestrator reacts to.

**For the OS Service (F4-13/F4-22, the Saga orchestrator):**

- Declare records with exactly this namespace, name and fields — MassTransit identifies a message by
  `urn:message:TorqueOS.Contracts.Execution:<Name>`. Copy them from
  `src/MechanicsSoftware.ExecutionService.Application/Contracts/`.
- Each message has its own fanout exchange named `TorqueOS.Contracts.Execution:<Name>`; this service's consumers (for
  `EnqueueExecution` and `CancelExecution`) bind their queues to it automatically.
- `priority` is optional and currently unused by any real caller — every order queues at the same priority (plain
  FIFO by arrival) until the OS Service has a reason to set it.
- `CorrelationId` on every published message is the `ServiceOrderId`.
- Evolution: fields are only ever added. A breaking change gets a new type (e.g. `EnqueueExecutionV2`).

## Known infrastructure gap

`mechanics-infra-k8s`'s RabbitMQ provisioning (F4-08) gives each service its **own vhost**
(`/os`, `/billing`, `/execution`) with no cross-vhost federation/shovel configured. RabbitMQ vhosts are isolated
namespaces, so as provisioned today, a message published in one service's vhost cannot reach a consumer bound to
another vhost — which would block the real cross-service Saga once it's wired (F4-13/F4-22). Worth confirming with
whoever owns F4-08 before relying on it for the end-to-end demo.

## Project layout

```
src/MechanicsSoftware.ExecutionService.Domain/         ExecutionOrder, ExecutionStatus
src/MechanicsSoftware.ExecutionService.Application/     Handlers, IExecutionDbContext, IExecutionQueue, Contracts/
src/MechanicsSoftware.ExecutionService.Infrastructure/  EF Core (Postgres), Mongo queue, MassTransit consumer
src/MechanicsSoftware.ExecutionService.Api/             Controllers, Program.cs
tests/MechanicsSoftware.ExecutionService.UnitTests/
k8s/                                                    Namespace, Deployment, Service, ConfigMap, Secret, HPA
.github/workflows/ci.yml                                Build, test, Docker build (SonarQube + deploy: F4-30/F4-34)
```

## Contributing

`main` is protected — open a PR from a branch and get one approval. Conventional commits.
