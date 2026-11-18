using MechanicsSoftware.ExecutionService.Application.Abstractions;
using MechanicsSoftware.ExecutionService.Application.Exceptions;
using MechanicsSoftware.ExecutionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MechanicsSoftware.ExecutionService.Application.UseCases.Executions.Handlers;

/// <summary>Staff action: the repair is done. Publishes <c>ExecutionCompleted</c> (F4-20).</summary>
public sealed class CompleteExecutionHandler(IExecutionDbContext db, IExecutionEventPublisher publisher)
{
    public async Task<ExecutionOrderResponse> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await db.ExecutionOrders.FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(ExecutionOrder), id);

        order.Complete();

        // Publish before SaveChanges: the outbox writer picks up the message in the same
        // transaction (ADR-011), the same convention as the OS/Billing Service publishers.
        await publisher.PublishExecutionCompletedAsync(order, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return ExecutionOrderResponse.From(order);
    }
}
