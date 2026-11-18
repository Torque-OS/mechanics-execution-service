using MechanicsSoftware.ExecutionService.Domain.Entities;

namespace MechanicsSoftware.ExecutionService.Application.Abstractions;

/// <summary>Publishes the execution Saga events (ADR-011, F4-20).</summary>
public interface IExecutionEventPublisher
{
    Task PublishExecutionCompletedAsync(ExecutionOrder order, CancellationToken cancellationToken = default);

    Task PublishExecutionFailedAsync(ExecutionOrder order, CancellationToken cancellationToken = default);
}
