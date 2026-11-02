using System.Diagnostics.CodeAnalysis;
using MassTransit;
using MechanicsSoftware.ExecutionService.Application.UseCases.Executions.Handlers;
using MechanicsSoftware.ExecutionService.Domain.Exceptions;
using MechanicsSoftware.ExecutionService.Infrastructure.Persistence;
using TorqueOS.Contracts.Execution;

namespace MechanicsSoftware.ExecutionService.Infrastructure.Messaging;

/// <summary>Saga command from the OS Service (ADR-011, F4-18): enqueue the order for execution.</summary>
public sealed class EnqueueExecutionConsumer(EnqueueExecutionHandler handler) : IConsumer<EnqueueExecution>
{
    public Task Consume(ConsumeContext<EnqueueExecution> context) =>
        handler.ExecuteAsync(context.Message.ServiceOrderId, context.Message.Priority, context.CancellationToken);
}

/// <summary>
/// Queue <c>execution.enqueue-execution</c>, bound to the <c>EnqueueExecution</c> exchange. A
/// transient failure (e.g. the database being briefly unavailable) is retried; a business
/// conflict (none expected — the handler is idempotent and never throws on a duplicate) is not.
/// A message that still fails goes to <c>execution.enqueue-execution_error</c>. The EF Core inbox
/// drops redeliveries of an already handled <c>MessageId</c>, and the outbox is reserved for this
/// service's own published events (F4-20).
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Endpoint wiring needs PostgreSQL and RabbitMQ; exercised manually against the compose stack.")]
public sealed class EnqueueExecutionConsumerDefinition : ConsumerDefinition<EnqueueExecutionConsumer>
{
    public const string QueueName = "execution.enqueue-execution";

    public EnqueueExecutionConsumerDefinition() => EndpointName = QueueName;

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<EnqueueExecutionConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry =>
        {
            retry.Intervals(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15));
            retry.Ignore<DomainException>();
        });

        endpointConfigurator.UseEntityFrameworkOutbox<ExecutionDbContext>(context);
    }
}
