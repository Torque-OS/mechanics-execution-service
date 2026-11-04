using MechanicsSoftware.ExecutionService.Application.Abstractions;
using MechanicsSoftware.ExecutionService.Application.Exceptions;
using MechanicsSoftware.ExecutionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MechanicsSoftware.ExecutionService.Application.UseCases.Executions.Handlers;

/// <summary>Staff action: diagnosis/repair work begins. Leaves the queue (F4-19).</summary>
public sealed class StartDiagnosisHandler(IExecutionDbContext db, IExecutionQueue queue)
{
    public async Task<ExecutionOrderResponse> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await db.ExecutionOrders.FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(ExecutionOrder), id);

        order.StartDiagnosis();
        await db.SaveChangesAsync(cancellationToken);

        await queue.RemoveAsync(order.Id, cancellationToken);

        return ExecutionOrderResponse.From(order);
    }
}
