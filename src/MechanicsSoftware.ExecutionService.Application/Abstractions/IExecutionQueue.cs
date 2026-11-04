namespace MechanicsSoftware.ExecutionService.Application.Abstractions;

/// <summary>Read-optimized queue projection in MongoDB (ADR-012, collection <c>execution_queue</c>).</summary>
public sealed record ExecutionQueueEntry(
    Guid ExecutionOrderId,
    Guid ServiceOrderId,
    int Priority,
    DateTime QueuedAt);

public interface IExecutionQueue
{
    Task EnqueueAsync(ExecutionQueueEntry entry, CancellationToken cancellationToken = default);

    /// <summary>Current queue, ordered by <see cref="ExecutionQueueEntry.Priority"/> then <see cref="ExecutionQueueEntry.QueuedAt"/>.</summary>
    Task<IReadOnlyList<ExecutionQueueEntry>> GetQueueAsync(CancellationToken cancellationToken = default);

    Task RemoveAsync(Guid executionOrderId, CancellationToken cancellationToken = default);
}
