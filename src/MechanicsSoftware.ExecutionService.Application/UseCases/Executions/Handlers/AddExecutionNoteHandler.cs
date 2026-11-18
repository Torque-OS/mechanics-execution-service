using MechanicsSoftware.ExecutionService.Application.Abstractions;
using MechanicsSoftware.ExecutionService.Application.Exceptions;
using MechanicsSoftware.ExecutionService.Domain.Entities;
using MechanicsSoftware.ExecutionService.Domain.Exceptions;
using MechanicsSoftware.ExecutionService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace MechanicsSoftware.ExecutionService.Application.UseCases.Executions.Handlers;

/// <summary>Records a diagnosis/repair note (F4-19). Only while the order is being worked on.</summary>
public sealed class AddExecutionNoteHandler(IExecutionDbContext db, IExecutionNotes notes)
{
    public async Task ExecuteAsync(Guid id, string text, CancellationToken cancellationToken = default)
    {
        var order = await db.ExecutionOrders.FirstOrDefaultAsync(e => e.Id == id, cancellationToken)
            ?? throw new NotFoundException(nameof(ExecutionOrder), id);

        if (string.IsNullOrWhiteSpace(text))
            throw new DomainException("Note text is required.");

        if (!order.Status.Is(ExecutionStatus.Status.InProgress))
            throw new DomainException(
                $"Notes can only be added while the order is IN_PROGRESS. Current status: {order.Status}.");

        await notes.AddAsync(new ExecutionNote(order.Id, text, DateTime.UtcNow), cancellationToken);
    }
}
