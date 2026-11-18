using MechanicsSoftware.ExecutionService.Application.Abstractions;
using MechanicsSoftware.ExecutionService.Application.Exceptions;
using MechanicsSoftware.ExecutionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MechanicsSoftware.ExecutionService.Application.UseCases.Executions.Handlers;

/// <summary>Staff action: the repair could not be finished. Publishes <c>ExecutionFailed</c> (F4-20).</summary>
public sealed class FailExecutionHandler(IExecutionDbContext db, IExecutionEventPublisher publisher)
{
    public async Task<ExecutionOrderResponse> ExecuteAsync(
        Guid id, string reason, CancellationToken cancellationToken = default)
    {
        var order = await db.ExecutionOrders.FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(ExecutionOrder), id);

        order.Fail(reason);

        await publisher.PublishExecutionFailedAsync(order, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return ExecutionOrderResponse.From(order);
    }
}
