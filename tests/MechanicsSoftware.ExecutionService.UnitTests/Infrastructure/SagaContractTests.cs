using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using MechanicsSoftware.ExecutionService.Domain.Entities;
using MechanicsSoftware.ExecutionService.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using TorqueOS.Contracts.Execution;

namespace MechanicsSoftware.ExecutionService.UnitTests.Infrastructure;

/// <summary>
/// Publishes through MassTransit's in-memory test harness. These tests pin the cross-service
/// contract: the OS Service (F4-13/F4-22) consumes these exact message identities and fields.
/// </summary>
public sealed class SagaContractTests : IAsyncLifetime
{
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;

    public async Task InitializeAsync()
    {
        _provider = new ServiceCollection().AddMassTransitTestHarness().BuildServiceProvider(true);
        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Theory]
    [InlineData(typeof(EnqueueExecution), "urn:message:TorqueOS.Contracts.Execution:EnqueueExecution")]
    [InlineData(typeof(ExecutionCompleted), "urn:message:TorqueOS.Contracts.Execution:ExecutionCompleted")]
    [InlineData(typeof(ExecutionFailed), "urn:message:TorqueOS.Contracts.Execution:ExecutionFailed")]
    public void MessageIdentity_IsStable(Type contract, string expectedUrn)
    {
        MessageUrn.ForType(contract).ToString().Should().Be(expectedUrn);
    }

    [Fact]
    public async Task ExecutionCompleted_CarriesContractFieldsAndServiceOrderCorrelation()
    {
        var order = ExecutionOrder.Create(Guid.NewGuid());
        order.StartDiagnosis();
        order.Complete();

        await new MassTransitExecutionEventPublisher(_harness.Bus).PublishExecutionCompletedAsync(order);

        var published = _harness.Published.Select<ExecutionCompleted>().Single().Context;
        published.Message.Should().Be(new ExecutionCompleted(order.ServiceOrderId, order.Id, order.FinishedAt!.Value));
        published.CorrelationId.Should().Be(order.ServiceOrderId);
        published.MessageId.Should().NotBeNull();
    }

    [Fact]
    public async Task ExecutionFailed_CarriesReasonAndServiceOrderCorrelation()
    {
        var order = ExecutionOrder.Create(Guid.NewGuid());
        order.StartDiagnosis();
        order.Fail("Part unavailable, customer unreachable for approval.");

        await new MassTransitExecutionEventPublisher(_harness.Bus).PublishExecutionFailedAsync(order);

        var published = _harness.Published.Select<ExecutionFailed>().Single().Context;
        published.Message.Should().Be(new ExecutionFailed(
            order.ServiceOrderId, order.Id, order.FailureReason!, order.FinishedAt!.Value));
        published.CorrelationId.Should().Be(order.ServiceOrderId);
    }
}
