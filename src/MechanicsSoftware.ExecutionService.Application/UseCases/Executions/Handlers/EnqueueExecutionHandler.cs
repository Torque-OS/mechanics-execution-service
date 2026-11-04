using MechanicsSoftware.ExecutionService.Application.Abstractions;
using MechanicsSoftware.ExecutionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MechanicsSoftware.ExecutionService.Application.UseCases.Executions.Handlers;

/// <summary>
/// Handles the <c>EnqueueExecution</c> saga command (ADR-011, F4-18). Invoked by
/// <c>EnqueueExecutionConsumer</c>; kept as a plain handler (not consumer logic) so it is
/// testable without a broker, matching the Billing Service's handler/consumer split.
/// </summary>
public sealed class EnqueueExecutionHandler(IExecutionDbContext db, IExecutionQueue queue)
{
    public async Task<ExecutionOrderResponse> ExecuteAsync(
        Guid serviceOrderId, int priority, CancellationToken cancellationToken = default)
    {
        // Idempotent: a redelivered or duplicate EnqueueExecution for the same order is a no-op.
        // (Broker-level redelivery of the same MessageId is already deduplicated by the EF Core
        // inbox before this handler runs — this guards against the OS Service sending the
        // command twice with different MessageIds.)
        var existing = await db.ExecutionOrders
            .FirstOrDefaultAsync(e => e.ServiceOrderId == serviceOrderId, cancellationToken);

        if (existing is not null)
            return ExecutionOrderResponse.From(existing);

        var order = ExecutionOrder.Create(serviceOrderId, priority);
        db.ExecutionOrders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        await queue.EnqueueAsync(
            new ExecutionQueueEntry(order.Id, order.ServiceOrderId, order.Priority, order.QueuedAt),
            cancellationToken);

        return ExecutionOrderResponse.From(order);
    }
}
