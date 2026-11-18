using FluentAssertions;
using MechanicsSoftware.ExecutionService.Application.Abstractions;
using MechanicsSoftware.ExecutionService.Application.Exceptions;
using MechanicsSoftware.ExecutionService.Application.UseCases.Executions.Handlers;
using MechanicsSoftware.ExecutionService.Domain.Entities;
using MechanicsSoftware.ExecutionService.UnitTests.Helpers;

namespace MechanicsSoftware.ExecutionService.UnitTests.Application.Executions;

public class StartDiagnosisHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_QueuedOrder_StatusBecomesInProgress()
    {
        await using var db = InMemoryDbContextHelper.Create();
        var order = ExecutionOrder.Create(Guid.NewGuid());
        db.ExecutionOrders.Add(order);
        await db.SaveChangesAsync();
        var queue = new InMemoryExecutionQueue();
        await queue.EnqueueAsync(new ExecutionQueueEntry(order.Id, order.ServiceOrderId, order.Priority, order.QueuedAt));

        var result = await new StartDiagnosisHandler(db, queue).ExecuteAsync(order.Id);

        result.Status.Should().Be("IN_PROGRESS");
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

        await new StartDiagnosisHandler(db, queue).ExecuteAsync(order.Id);

        (await queue.GetQueueAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_UnknownId_ThrowsNotFoundException()
    {
        await using var db = InMemoryDbContextHelper.Create();
        var queue = new InMemoryExecutionQueue();

        var act = async () => await new StartDiagnosisHandler(db, queue).ExecuteAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
