// Saga event published by the Execution Service (ADR-011, F4-20).
// The namespace is part of the message identity in MassTransit (urn:message:TorqueOS.Contracts.Execution:<Name>).
// Evolution: add fields only; a breaking change gets a new type (e.g. <Name>V2).
namespace TorqueOS.Contracts.Execution;

/// <summary>
/// The repair could not be finished — triggers the Saga compensation path (ADR-011): the OS
/// Service's orchestrator asks the Billing Service to refund the payment, and (F4-21) tells this
/// service to cancel the order.
/// </summary>
public sealed record ExecutionFailed(Guid ServiceOrderId, Guid ExecutionOrderId, string Reason, DateTime FailedAt);
