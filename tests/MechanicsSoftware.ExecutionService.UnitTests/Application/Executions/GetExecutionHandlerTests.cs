using FluentAssertions;
using MechanicsSoftware.ExecutionService.Application.Exceptions;
using MechanicsSoftware.ExecutionService.Application.UseCases.Executions.Handlers;
using MechanicsSoftware.ExecutionService.Domain.Entities;
using MechanicsSoftware.ExecutionService.UnitTests.Helpers;

namespace MechanicsSoftware.ExecutionService.UnitTests.Application.Executions;

public class GetExecutionHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_ExistingOrder_ReturnsIt()
    {
        await using var db = InMemoryDbContextHelper.Create();
        var order = ExecutionOrder.Create(Guid.NewGuid());
        db.ExecutionOrders.Add(order);
        await db.SaveChangesAsync();

        var result = await new GetExecutionHandler(db).ExecuteAsync(order.Id);

        result.Id.Should().Be(order.Id);
    }

    [Fact]
    public async Task ExecuteAsync_UnknownId_ThrowsNotFoundException()
    {
        await using var db = InMemoryDbContextHelper.Create();

        var act = async () => await new GetExecutionHandler(db).ExecuteAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
