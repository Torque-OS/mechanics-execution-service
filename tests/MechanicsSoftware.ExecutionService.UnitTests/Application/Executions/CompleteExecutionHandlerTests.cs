using FluentAssertions;
using MechanicsSoftware.ExecutionService.Application.Exceptions;
using MechanicsSoftware.ExecutionService.Application.UseCases.Executions.Handlers;
using MechanicsSoftware.ExecutionService.Domain.Entities;
using MechanicsSoftware.ExecutionService.Domain.Exceptions;
using MechanicsSoftware.ExecutionService.UnitTests.Helpers;

namespace MechanicsSoftware.ExecutionService.UnitTests.Application.Executions;

public class CompleteExecutionHandlerTests
{
    private static ExecutionOrder InProgressOrder()
    {
        var order = ExecutionOrder.Create(Guid.NewGuid());
        order.StartDiagnosis();
        return order;
    }

    [Fact]
    public async Task ExecuteAsync_InProgressOrder_StatusBecomesCompleted()
    {
        await using var db = InMemoryDbContextHelper.Create();
        var order = InProgressOrder();
        db.ExecutionOrders.Add(order);
        await db.SaveChangesAsync();
        var publisher = new RecordingExecutionEventPublisher();

        var result = await new CompleteExecutionHandler(db, publisher).ExecuteAsync(order.Id);

        result.Status.Should().Be("COMPLETED");
    }

    [Fact]
    public async Task ExecuteAsync_InProgressOrder_PublishesExecutionCompleted()
    {
        await using var db = InMemoryDbContextHelper.Create();
        var order = InProgressOrder();
        db.ExecutionOrders.Add(order);
        await db.SaveChangesAsync();
        var publisher = new RecordingExecutionEventPublisher();

        await new CompleteExecutionHandler(db, publisher).ExecuteAsync(order.Id);

        publisher.Completed.Should().ContainSingle(o => o.Id == order.Id);
    }

    [Fact]
    public async Task ExecuteAsync_StillQueued_ThrowsDomainException()
    {
        await using var db = InMemoryDbContextHelper.Create();
        var order = ExecutionOrder.Create(Guid.NewGuid());
        db.ExecutionOrders.Add(order);
        await db.SaveChangesAsync();
        var publisher = new RecordingExecutionEventPublisher();

        var act = async () => await new CompleteExecutionHandler(db, publisher).ExecuteAsync(order.Id);

        await act.Should().ThrowAsync<DomainException>();
        publisher.Completed.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_UnknownId_ThrowsNotFoundException()
    {
        await using var db = InMemoryDbContextHelper.Create();
        var publisher = new RecordingExecutionEventPublisher();

        var act = async () => await new CompleteExecutionHandler(db, publisher).ExecuteAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
