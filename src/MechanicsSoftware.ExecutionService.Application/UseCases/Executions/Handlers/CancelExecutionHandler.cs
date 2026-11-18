using MechanicsSoftware.ExecutionService.Application.Abstractions;
using MechanicsSoftware.ExecutionService.Application.Exceptions;
using MechanicsSoftware.ExecutionService.Domain.Entities;
using MechanicsSoftware.ExecutionService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace MechanicsSoftware.ExecutionService.Application.UseCases.Executions.Handlers;

/// <summary>Handles the <c>CancelExecution</c> saga compensation command (ADR-011, F4-21).</summary>
public sealed class CancelExecutionHandler(IExecutionDbContext db, IExecutionQueue queue)
{
    public async Task ExecuteAsync(Guid serviceOrderId, CancellationToken cancellationToken = default)
    {
        var order = await db.ExecutionOrders.FirstOrDefaultAsync(e => e.ServiceOrderId == serviceOrderId, cancellationToken)
            ?? throw new NotFoundException(nameof(ExecutionOrder), serviceOrderId);

        // Idempotent: a redelivered or repeated CancelExecution for an already cancelled order
        // is a no-op (Status.TransitionTo would otherwise reject Cancelled -> Cancelled).
        if (order.Status.Is(ExecutionStatus.Status.Cancelled))
            return;

        order.Cancel();
        await db.SaveChangesAsync(cancellationToken);

        await queue.RemoveAsync(order.Id, cancellationToken);
    }
}
