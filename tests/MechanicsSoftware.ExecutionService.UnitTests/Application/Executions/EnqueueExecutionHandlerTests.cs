using FluentAssertions;
using MechanicsSoftware.ExecutionService.Application.UseCases.Executions.Handlers;
using MechanicsSoftware.ExecutionService.UnitTests.Helpers;

namespace MechanicsSoftware.ExecutionService.UnitTests.Application.Executions;

public class EnqueueExecutionHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_NewServiceOrder_CreatesQueuedExecutionOrder()
    {
        await using var db = InMemoryDbContextHelper.Create();
        var queue = new InMemoryExecutionQueue();
        var serviceOrderId = Guid.NewGuid();

        var result = await new EnqueueExecutionHandler(db, queue).ExecuteAsync(serviceOrderId, priority: 0);

        result.ServiceOrderId.Should().Be(serviceOrderId);
        result.Status.Should().Be("QUEUED");
    }

    [Fact]
    public async Task ExecuteAsync_NewServiceOrder_PersistsToDatabase()
    {
        await using var db = InMemoryDbContextHelper.Create();
        var queue = new InMemoryExecutionQueue();
        var serviceOrderId = Guid.NewGuid();

        await new EnqueueExecutionHandler(db, queue).ExecuteAsync(serviceOrderId, priority: 0);

        db.ExecutionOrders.Should().ContainSingle(e => e.ServiceOrderId == serviceOrderId);
    }

    [Fact]
    public async Task ExecuteAsync_NewServiceOrder_AddsToQueue()
    {
        await using var db = InMemoryDbContextHelper.Create();
        var queue = new InMemoryExecutionQueue();
        var serviceOrderId = Guid.NewGuid();

        await new EnqueueExecutionHandler(db, queue).ExecuteAsync(serviceOrderId, priority: 3);

        var entries = await queue.GetQueueAsync();
        entries.Should().ContainSingle(e => e.ServiceOrderId == serviceOrderId && e.Priority == 3);
    }

    [Fact]
    public async Task ExecuteAsync_DuplicateServiceOrder_IsIdempotent()
    {
        await using var db = InMemoryDbContextHelper.Create();
        var queue = new InMemoryExecutionQueue();
        var handler = new EnqueueExecutionHandler(db, queue);
        var serviceOrderId = Guid.NewGuid();

        var first = await handler.ExecuteAsync(serviceOrderId, priority: 0);
        var second = await handler.ExecuteAsync(serviceOrderId, priority: 0);

        second.Id.Should().Be(first.Id);
        db.ExecutionOrders.Should().HaveCount(1);
    }
}
