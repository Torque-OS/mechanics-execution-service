using MechanicsSoftware.ExecutionService.Domain.Entities;

namespace MechanicsSoftware.ExecutionService.Application.UseCases.Executions;

public sealed record ExecutionOrderResponse(
    Guid Id,
    Guid ServiceOrderId,
    string Status,
    int Priority,
    DateTime QueuedAt)
{
    public static ExecutionOrderResponse From(ExecutionOrder order) =>
        new(order.Id, order.ServiceOrderId, order.Status.ToString(), order.Priority, order.QueuedAt);
}
