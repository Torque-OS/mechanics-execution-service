using MassTransit;
using MechanicsSoftware.ExecutionService.Application.Abstractions;
using MechanicsSoftware.ExecutionService.Domain.Entities;
using TorqueOS.Contracts.Execution;

namespace MechanicsSoftware.ExecutionService.Infrastructure.Messaging;

/// <summary>
/// Publishes the execution Saga events (ADR-011, F4-20). <see cref="IPublishEndpoint"/> is scoped
/// to the request and backed by the EF Core bus outbox: messages are stored with the next
/// <c>SaveChanges</c> and delivered to RabbitMQ after it commits. The service order id is the
/// correlation id of every message.
/// </summary>
public sealed class MassTransitExecutionEventPublisher(IPublishEndpoint bus) : IExecutionEventPublisher
{
    public Task PublishExecutionCompletedAsync(ExecutionOrder order, CancellationToken cancellationToken = default) =>
        Publish(new ExecutionCompleted(order.ServiceOrderId, order.Id, order.FinishedAt ?? DateTime.UtcNow),
            order.ServiceOrderId, cancellationToken);

    public Task PublishExecutionFailedAsync(ExecutionOrder order, CancellationToken cancellationToken = default) =>
        Publish(
            new ExecutionFailed(
                order.ServiceOrderId, order.Id, order.FailureReason ?? "Unknown reason",
                order.FinishedAt ?? DateTime.UtcNow),
            order.ServiceOrderId, cancellationToken);

    private Task Publish<T>(T message, Guid serviceOrderId, CancellationToken cancellationToken) where T : class =>
        bus.Publish(message, context => context.CorrelationId = serviceOrderId, cancellationToken);
}
