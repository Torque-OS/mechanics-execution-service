// Saga contract consumed by the Execution Service (ADR-011, F4-18).
// The namespace is part of the message identity in MassTransit (urn:message:TorqueOS.Contracts.Execution:<Name>):
// the OS Service must declare this exact namespace and type name when sending it (see
// TorqueOS.Contracts.Billing.CancelBudget in mechanics-billing-service for the same convention).
// Evolution: add fields only; a breaking change gets a new type (e.g. <Name>V2).
namespace TorqueOS.Contracts.Execution;

/// <summary>
/// Command from the OS Service's Saga orchestrator: payment was confirmed, so enqueue the order
/// for diagnosis/repair. <paramref name="Priority"/> is optional (default 0, plain FIFO); the OS
/// Service does not send a priority hint yet. Idempotent — re-enqueuing a known
/// <paramref name="ServiceOrderId"/> is a no-op (see <c>EnqueueExecutionHandler</c>).
/// </summary>
public sealed record EnqueueExecution(Guid ServiceOrderId, int Priority = 0);
