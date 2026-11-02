// Saga contract consumed by the Execution Service (ADR-011, F4-21).
// The namespace is part of the message identity in MassTransit (urn:message:TorqueOS.Contracts.Execution:<Name>):
// the OS Service must declare this exact namespace and type name when sending it (see
// TorqueOS.Contracts.Billing.CancelBudget in mechanics-billing-service for the same convention).
// Evolution: add fields only; a breaking change gets a new type (e.g. <Name>V2).
namespace TorqueOS.Contracts.Execution;

/// <summary>
/// Compensation command from the OS Service: the order was cancelled, so revert the queue entry
/// or in-progress work. Sent only while the order may still be cancelled (never after
/// <c>ExecutionCompleted</c> was already published). Idempotent — repeating it for an already
/// cancelled order is a no-op.
/// </summary>
public sealed record CancelExecution(Guid ServiceOrderId);
