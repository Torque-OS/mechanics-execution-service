using FluentAssertions;
using MechanicsSoftware.ExecutionService.Application.Abstractions;
using MechanicsSoftware.ExecutionService.Application.Exceptions;
using MechanicsSoftware.ExecutionService.Application.UseCases.Executions.Handlers;
using MechanicsSoftware.ExecutionService.Domain.Entities;
using MechanicsSoftware.ExecutionService.UnitTests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace MechanicsSoftware.ExecutionService.UnitTests.Application.Executions;

public class CancelExecutionHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_QueuedOrder_StatusBecomesCancelled()
    {
        await using var db = InMemoryDbContextHelper.Create();
        var order = ExecutionOrder.Create(Guid.NewGuid());
        db.ExecutionOrders.Add(order);
        await db.SaveChangesAsync();
        var queue = new InMemoryExecutionQueue();
        await queue.EnqueueAsync(new ExecutionQueueEntry(order.Id, order.ServiceOrderId, order.Priority, order.QueuedAt));

        await new CancelExecutionHandler(db, queue).ExecuteAsync(order.ServiceOrderId);

        var stored = await db.ExecutionOrders.SingleAsync();
        stored.Status.ToString().Should().Be("CANCELLED");
    }

    [Fact]
    public async Task ExecuteAsync_QueuedOrder_RemovesFromQueue()
    {
        await using var db = InMemoryDbContextHelper.Create();
        var order = ExecutionOrder.Create(Guid.NewGuid());
        db.ExecutionOrders.Add(order);
        await db.SaveChangesAsync();
        var queue = new InMemoryExecutionQueue();
        await queue.EnqueueAsync(new ExecutionQueueEntry(order.Id, order.ServiceOrderId, order.Priority, order.QueuedAt));

        await new CancelExecutionHandler(db, queue).ExecuteAsync(order.ServiceOrderId);

        (await queue.GetQueueAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_InProgressOrder_StatusBecomesCancelled()
    {
        await using var db = InMemoryDbContextHelper.Create();
        var order = ExecutionOrder.Create(Guid.NewGuid());
        order.StartDiagnosis();
        db.ExecutionOrders.Add(order);
        await db.SaveChangesAsync();
        var queue = new InMemoryExecutionQueue();

        await new CancelExecutionHandler(db, queue).ExecuteAsync(order.ServiceOrderId);

        var stored = await db.ExecutionOrders.SingleAsync();
        stored.Status.ToString().Should().Be("CANCELLED");
    }

    [Fact]
    public async Task ExecuteAsync_AlreadyCancelled_IsIdempotent()
    {
        await using var db = InMemoryDbContextHelper.Create();
        var order = ExecutionOrder.Create(Guid.NewGuid());
        order.Cancel();
        db.ExecutionOrders.Add(order);
        await db.SaveChangesAsync();
        var queue = new InMemoryExecutionQueue();

        var act = async () => await new CancelExecutionHandler(db, queue).ExecuteAsync(order.ServiceOrderId);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteAsync_UnknownServiceOrder_ThrowsNotFoundException()
    {
        await using var db = InMemoryDbContextHelper.Create();
        var queue = new InMemoryExecutionQueue();

        var act = async () => await new CancelExecutionHandler(db, queue).ExecuteAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
