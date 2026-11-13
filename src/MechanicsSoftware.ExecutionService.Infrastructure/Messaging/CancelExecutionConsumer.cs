using System.Diagnostics.CodeAnalysis;
using MassTransit;
using MechanicsSoftware.ExecutionService.Application.Exceptions;
using MechanicsSoftware.ExecutionService.Application.UseCases.Executions.Handlers;
using MechanicsSoftware.ExecutionService.Domain.Exceptions;
using MechanicsSoftware.ExecutionService.Infrastructure.Persistence;
using TorqueOS.Contracts.Execution;

namespace MechanicsSoftware.ExecutionService.Infrastructure.Messaging;

/// <summary>Saga compensation command from the OS Service (ADR-011, F4-21).</summary>
public sealed class CancelExecutionConsumer(CancelExecutionHandler handler) : IConsumer<CancelExecution>
{
    public Task Consume(ConsumeContext<CancelExecution> context) =>
        handler.ExecuteAsync(context.Message.ServiceOrderId, context.CancellationToken);
}

/// <summary>
/// Queue <c>execution.cancel-execution</c>, bound to the <c>CancelExecution</c> exchange. A
/// transient failure (e.g. the database being briefly unavailable) is retried; an unknown
/// service order is not — it goes straight to <c>execution.cancel-execution_error</c>. The EF
/// Core inbox drops redeliveries of an already handled <c>MessageId</c>; the handler itself is
/// idempotent for a plain repeat of the command.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Endpoint wiring needs PostgreSQL and RabbitMQ; exercised manually against the compose stack.")]
public sealed class CancelExecutionConsumerDefinition : ConsumerDefinition<CancelExecutionConsumer>
{
    public const string QueueName = "execution.cancel-execution";

    public CancelExecutionConsumerDefinition() => EndpointName = QueueName;

    protected override void ConfigureConsumer(
        IReceiveEndpointConfigurator endpointConfigurator,
        IConsumerConfigurator<CancelExecutionConsumer> consumerConfigurator,
        IRegistrationContext context)
    {
        endpointConfigurator.UseMessageRetry(retry =>
        {
            retry.Intervals(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15));
            retry.Ignore<DomainException>();
            retry.Ignore<NotFoundException>();
        });

        endpointConfigurator.UseEntityFrameworkOutbox<ExecutionDbContext>(context);
    }
}
