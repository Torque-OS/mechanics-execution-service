using FluentAssertions;
using MassTransit;
using MassTransit.Testing;
using MechanicsSoftware.ExecutionService.Application.Abstractions;
using MechanicsSoftware.ExecutionService.Application.UseCases.Executions.Handlers;
using MechanicsSoftware.ExecutionService.Domain.Entities;
using MechanicsSoftware.ExecutionService.Infrastructure.Messaging;
using MechanicsSoftware.ExecutionService.Infrastructure.Persistence;
using MechanicsSoftware.ExecutionService.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TorqueOS.Contracts.Execution;

namespace MechanicsSoftware.ExecutionService.UnitTests.Infrastructure;

/// <summary>
/// The Saga compensation path end to end in memory: <c>CancelExecution</c> in, the order
/// cancelled. Retry, inbox and outbox (<see cref="CancelExecutionConsumerDefinition"/>) need
/// PostgreSQL and are checked against the compose stack.
/// </summary>
public sealed class CancelExecutionConsumerTests : IAsyncLifetime
{
    private readonly string _databaseName = Guid.NewGuid().ToString();
    private ServiceProvider _provider = null!;
    private ITestHarness _harness = null!;

    public Task InitializeAsync()
    {
        _provider = new ServiceCollection()
            .AddDbContext<ExecutionDbContext>(options => options.UseInMemoryDatabase(_databaseName))
            .AddScoped<IExecutionDbContext>(sp => sp.GetRequiredService<ExecutionDbContext>())
            .AddSingleton<IExecutionQueue>(new InMemoryExecutionQueue())
            .AddScoped<CancelExecutionHandler>()
            .AddMassTransitTestHarness(bus => bus.AddConsumer<CancelExecutionConsumer>())
            .BuildServiceProvider(true);
        _harness = _provider.GetRequiredService<ITestHarness>();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync() => await _provider.DisposeAsync();

    private async Task<ExecutionOrder> SeedQueuedOrderAsync()
    {
        await using var scope = _provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ExecutionDbContext>();
        var order = ExecutionOrder.Create(Guid.NewGuid());
        db.ExecutionOrders.Add(order);
        await db.SaveChangesAsync();
        return order;
    }

    [Fact]
    public async Task CancelExecution_CancelsTheOrder()
    {
        var order = await SeedQueuedOrderAsync();
        await _harness.Start();

        await _harness.Bus.Publish(new CancelExecution(order.ServiceOrderId));

        (await _harness.Consumed.Any<CancelExecution>()).Should().BeTrue();

        await using var scope = _provider.CreateAsyncScope();
        var stored = await scope.ServiceProvider.GetRequiredService<ExecutionDbContext>().ExecutionOrders.SingleAsync();
        stored.Status.ToString().Should().Be("CANCELLED");
    }

    [Fact]
    public async Task CancelExecution_UnknownServiceOrder_FaultsWithoutThrowingUnhandled()
    {
        await _harness.Start();

        await _harness.Bus.Publish(new CancelExecution(Guid.NewGuid()));

        (await _harness.Published.Any<Fault<CancelExecution>>()).Should().BeTrue();
    }
}
