namespace MechanicsSoftware.ExecutionService.Application.Abstractions;

/// <summary>Diagnosis/repair note in MongoDB (ADR-012, collection <c>execution_notes</c>, append-only).</summary>
public sealed record ExecutionNote(Guid ExecutionOrderId, string Text, DateTime RecordedAt);

public interface IExecutionNotes
{
    Task AddAsync(ExecutionNote note, CancellationToken cancellationToken = default);

    /// <summary>Notes for an order, oldest first.</summary>
    Task<IReadOnlyList<ExecutionNote>> ListAsync(Guid executionOrderId, CancellationToken cancellationToken = default);
}
