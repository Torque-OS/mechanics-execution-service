# mechanics-execution-service

Execution Service — one of the three microservices of the [Mechanics Software](https://github.com/Torque-OS/mechanics-software) Fase 4 architecture (FIAP POS Tech 15SOAT).

## Responsibility

Per [ADR-010](https://github.com/Torque-OS/mechanics-software/blob/main/docs/decisions/ADR-010-microservices-split.md):

- Manage the execution queue for service orders once payment is confirmed.
- Track diagnosis and repair status (F4-19).
- React to the `EnqueueExecution` command (**done, F4-18**) and publish `ExecutionCompleted`/`ExecutionFailed`
  events (F4-20) to the OS Service's Saga orchestrator ([ADR-011](https://github.com/Torque-OS/mechanics-software/blob/main/docs/decisions/ADR-011-saga-orquestrada-e-mensageria.md)).

This repo owns its own data — **no other service may query this database directly**
([ADR-012](https://github.com/Torque-OS/mechanics-software/blob/main/docs/decisions/ADR-012-banco-por-servico.md)): PostgreSQL (`execution_orders`, system of record) and MongoDB (`execution_queue`, a read-optimized
projection of the queue — see [Persistence](#persistence)).

## Status

- **Scaffold (F4-06): done.**
- **Execution queue (F4-18): done.** `EnqueueExecution` saga command in, queue entry out — see
  [Saga messages](#saga-messages). No broker/RabbitMQ provisioning in the cluster yet (F4-08);
  local development uses this repo's own `docker compose` RabbitMQ.
- Diagnosis/repair status updates, publishing `ExecutionCompleted`/`ExecutionFailed`, and the
  compensation path are **F4-19 to F4-21** — not implemented yet. See the
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
- **MongoDB** (`execution_queue`): a denormalized, priority-ordered view written alongside the
  Postgres row. Reads for "what's next" don't compete with the transactional writes, and the
  document shape can evolve in F4-19 (diagnosis notes) without a relational migration. This is a
  read projection, not a queue broker — nothing dequeues from it yet (F4-19/F4-21 will, when a
  diagnosis starts or a compensation cancels an entry).

## Endpoints

| Method | Path | |
|---|---|---|
| GET | `/api/executions` | Current queue, ordered by priority then arrival time |
| GET | `/api/executions/{id}` | A single execution order |
| GET | `/health` | Liveness/readiness probe |

## Saga messages

The Execution Service **consumes** this command (ADR-011) with MassTransit 8.5 over RabbitMQ, through the **EF Core
transactional inbox**: a redelivery of the same `MessageId` is dropped before the handler runs.

| Message | Direction | Payload |
|---|---|---|
| `EnqueueExecution` | OS Service → Execution Service | `{ serviceOrderId: guid, priority: int = 0 }` |

- Idempotent: re-sending `EnqueueExecution` for an already-queued `serviceOrderId` is a no-op (enforced by a unique
  index, not just the inbox).
- Database failures are retried (1 s, 5 s, 15 s). A message that still fails goes to
  `execution.enqueue-execution_error`; move it back with the RabbitMQ shovel/"Move messages" once the cause is fixed.
- Local test without the OS Service — RabbitMQ UI → *Exchanges* → `TorqueOS.Contracts.Execution:EnqueueExecution` →
  *Publish message*, property `content_type` = `application/vnd.masstransit+json`, payload:

  ```json
  { "messageId": "<new guid>", "messageType": ["urn:message:TorqueOS.Contracts.Execution:EnqueueExecution"],
    "message": { "serviceOrderId": "<guid>", "priority": 0 } }
  ```

**For the OS Service (F4-13/F4-22, the Saga orchestrator):**

- Declare a record with exactly this namespace, name and fields — MassTransit identifies a message by
  `urn:message:TorqueOS.Contracts.Execution:<Name>`. Copy it from
  `src/MechanicsSoftware.ExecutionService.Application/Contracts/EnqueueExecution.cs`.
- The message has its own fanout exchange named `TorqueOS.Contracts.Execution:EnqueueExecution`; this service's
  consumer binds its queue to it automatically.
- `priority` is optional and currently unused by any real caller — every order queues at the same priority (plain
  FIFO by arrival) until the OS Service has a reason to set it.
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
