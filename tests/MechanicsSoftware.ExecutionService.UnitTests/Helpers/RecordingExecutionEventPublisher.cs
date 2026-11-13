using MechanicsSoftware.ExecutionService.Application.Abstractions;
using MechanicsSoftware.ExecutionService.Domain.Entities;

namespace MechanicsSoftware.ExecutionService.UnitTests.Helpers;

public sealed class RecordingExecutionEventPublisher : IExecutionEventPublisher
{
    public List<ExecutionOrder> Completed { get; } = [];
    public List<ExecutionOrder> Failed { get; } = [];

    public Task PublishExecutionCompletedAsync(ExecutionOrder order, CancellationToken cancellationToken = default)
    {
        Completed.Add(order);
        return Task.CompletedTask;
    }

    public Task PublishExecutionFailedAsync(ExecutionOrder order, CancellationToken cancellationToken = default)
    {
        Failed.Add(order);
        return Task.CompletedTask;
    }
}
