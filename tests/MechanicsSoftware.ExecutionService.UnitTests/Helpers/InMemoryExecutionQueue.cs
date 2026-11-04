using MechanicsSoftware.ExecutionService.Application.Abstractions;

namespace MechanicsSoftware.ExecutionService.UnitTests.Helpers;

/// <summary>In-memory stand-in for the MongoDB-backed queue (<c>MongoExecutionQueue</c>), used in handler tests.</summary>
public sealed class InMemoryExecutionQueue : IExecutionQueue
{
    private readonly Dictionary<Guid, ExecutionQueueEntry> _entries = [];

    public Task EnqueueAsync(ExecutionQueueEntry entry, CancellationToken cancellationToken = default)
    {
        _entries[entry.ExecutionOrderId] = entry;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ExecutionQueueEntry>> GetQueueAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ExecutionQueueEntry> ordered = _entries.Values
            .OrderBy(e => e.Priority)
            .ThenBy(e => e.QueuedAt)
            .ToList();

        return Task.FromResult(ordered);
    }

    public Task RemoveAsync(Guid executionOrderId, CancellationToken cancellationToken = default)
    {
        _entries.Remove(executionOrderId);
        return Task.CompletedTask;
    }
}
