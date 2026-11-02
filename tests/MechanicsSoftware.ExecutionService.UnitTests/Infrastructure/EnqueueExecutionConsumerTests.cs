using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using MechanicsSoftware.ExecutionService.Application.Abstractions;
using MechanicsSoftware.ExecutionService.Application.UseCases.Executions.Handlers;
using MechanicsSoftware.ExecutionService.Infrastructure.Messaging;
using MechanicsSoftware.ExecutionService.Infrastructure.Persistence;
using MechanicsSoftware.ExecutionService.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TorqueOS.Contracts.Execution;

namespace MechanicsSoftware.ExecutionService.UnitTests.Infrastructure;

/// <summary>
/// The Saga path end to end in memory: <c>EnqueueExecution</c> in, an entry added to the queue.
/// Retry, inbox and outbox (<see cref="EnqueueExecutionConsumerDefinition"/>) need PostgreSQL and
/// are checked against the compose stack.
/// </summary>
public sealed class EnqueueExecutionConsumerTests : IAsyncLifetime
{
    private readonly string _databaseName = Guid.NewGuid().ToString();
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;
    private InMemoryExecutionQueue _queue = null!;

    public async Task InitializeAsync()
    {
        _queue = new InMemoryExecutionQueue();

        _provider = new ServiceCollection()
            .AddDbContext<ExecutionDbContext>(options => options.UseInMemoryDatabase(_databaseName))
            .AddScoped<IExecutionDbContext>(sp => sp.GetRequiredService<ExecutionDbContext>())
            .AddSingleton<IExecutionQueue>(_queue)
            .AddScoped<EnqueueExecutionHandler>()
            .AddMassTransitTestHarness(bus => bus.AddConsumer<EnqueueExecutionConsumer>())
            .BuildServiceProvider(true);
        _harness = _provider.GetRequiredService<ITestHarness>();
        await _harness.Start();
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    [Fact]
    public async Task EnqueueExecution_CreatesQueuedExecutionOrderAndAddsToQueue()
    {
        var serviceOrderId = Guid.NewGuid();

        await _harness.Bus.Publish(new EnqueueExecution(serviceOrderId, Priority: 2));

        (await _harness.Consumed.Any<EnqueueExecution>()).Should().BeTrue();

        await using var scope = _provider.CreateAsyncScope();
        var stored = await scope.ServiceProvider.GetRequiredService<ExecutionDbContext>()
            .ExecutionOrders.SingleAsync();
        stored.ServiceOrderId.Should().Be(serviceOrderId);
        stored.Priority.Should().Be(2);
        stored.Status.ToString().Should().Be("QUEUED");

        var queued = await _queue.GetQueueAsync();
        queued.Should().ContainSingle(e => e.ServiceOrderId == serviceOrderId);
    }

    [Fact]
    public async Task EnqueueExecution_DuplicateServiceOrder_StaysIdempotent()
    {
        var serviceOrderId = Guid.NewGuid();

        await _harness.Bus.Publish(new EnqueueExecution(serviceOrderId));
        await _harness.Bus.Publish(new EnqueueExecution(serviceOrderId));

        _harness.Consumed.Select<EnqueueExecution>().Count().Should().Be(2);

        await using var scope = _provider.CreateAsyncScope();
        var count = await scope.ServiceProvider.GetRequiredService<ExecutionDbContext>()
            .ExecutionOrders.CountAsync(e => e.ServiceOrderId == serviceOrderId);
        count.Should().Be(1);
    }
}
