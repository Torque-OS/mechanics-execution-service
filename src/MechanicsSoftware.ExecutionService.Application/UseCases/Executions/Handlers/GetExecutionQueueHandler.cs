using MechanicsSoftware.ExecutionService.Application.Abstractions;

namespace MechanicsSoftware.ExecutionService.Application.UseCases.Executions.Handlers;

/// <summary>Current execution queue, read from the MongoDB projection (ADR-012).</summary>
public sealed class GetExecutionQueueHandler(IExecutionQueue queue)
{
    public async Task<IReadOnlyList<ExecutionQueueEntryResponse>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var entries = await queue.GetQueueAsync(cancellationToken);
        return entries.Select(ExecutionQueueEntryResponse.From).ToList();
    }
}

public sealed record ExecutionQueueEntryResponse(
    Guid ExecutionOrderId,
    Guid ServiceOrderId,
    int Priority,
    DateTime QueuedAt)
{
    public static ExecutionQueueEntryResponse From(ExecutionQueueEntry entry) =>
        new(entry.ExecutionOrderId, entry.ServiceOrderId, entry.Priority, entry.QueuedAt);
}
