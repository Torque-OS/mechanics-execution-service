using FluentAssertions;
using MechanicsSoftware.ExecutionService.Application.Exceptions;
using MechanicsSoftware.ExecutionService.Application.UseCases.Executions.Handlers;
using MechanicsSoftware.ExecutionService.Domain.Entities;
using MechanicsSoftware.ExecutionService.Domain.Exceptions;
using MechanicsSoftware.ExecutionService.UnitTests.Helpers;

namespace MechanicsSoftware.ExecutionService.UnitTests.Application.Executions;

public class FailExecutionHandlerTests
{
    private static ExecutionOrder InProgressOrder()
    {
        var order = ExecutionOrder.Create(Guid.NewGuid());
        order.StartDiagnosis();
        return order;
    }

    [Fact]
    public async Task ExecuteAsync_InProgressOrder_StatusBecomesFailed()
    {
        await using var db = InMemoryDbContextHelper.Create();
        var order = InProgressOrder();
        db.ExecutionOrders.Add(order);
        await db.SaveChangesAsync();
        var publisher = new RecordingExecutionEventPublisher();

        var result = await new FailExecutionHandler(db, publisher).ExecuteAsync(order.Id, "Part unavailable");

        result.Status.Should().Be("FAILED");
        result.FailureReason.Should().Be("Part unavailable");
    }

    [Fact]
    public async Task ExecuteAsync_InProgressOrder_PublishesExecutionFailed()
    {
        await using var db = InMemoryDbContextHelper.Create();
        var order = InProgressOrder();
        db.ExecutionOrders.Add(order);
        await db.SaveChangesAsync();
        var publisher = new RecordingExecutionEventPublisher();

        await new FailExecutionHandler(db, publisher).ExecuteAsync(order.Id, "Part unavailable");

        publisher.Failed.Should().ContainSingle(o => o.Id == order.Id);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyReason_ThrowsDomainException()
    {
        await using var db = InMemoryDbContextHelper.Create();
        var order = InProgressOrder();
        db.ExecutionOrders.Add(order);
        await db.SaveChangesAsync();
        var publisher = new RecordingExecutionEventPublisher();

        var act = async () => await new FailExecutionHandler(db, publisher).ExecuteAsync(order.Id, "");

        await act.Should().ThrowAsync<DomainException>();
        publisher.Failed.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_UnknownId_ThrowsNotFoundException()
    {
        await using var db = InMemoryDbContextHelper.Create();
        var publisher = new RecordingExecutionEventPublisher();

        var act = async () => await new FailExecutionHandler(db, publisher).ExecuteAsync(Guid.NewGuid(), "reason");

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
