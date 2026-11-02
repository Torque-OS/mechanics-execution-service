using FluentAssertions;
using MechanicsSoftware.ExecutionService.Application.Abstractions;
using MechanicsSoftware.ExecutionService.Application.UseCases.Executions.Handlers;
using MechanicsSoftware.ExecutionService.UnitTests.Helpers;

namespace MechanicsSoftware.ExecutionService.UnitTests.Application.Executions;

public class GetExecutionQueueHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_OrdersByPriorityThenQueuedAt()
    {
        var queue = new InMemoryExecutionQueue();
        var now = DateTime.UtcNow;

        var low = new ExecutionQueueEntry(Guid.NewGuid(), Guid.NewGuid(), Priority: 5, QueuedAt: now);
        var high = new ExecutionQueueEntry(Guid.NewGuid(), Guid.NewGuid(), Priority: 1, QueuedAt: now.AddMinutes(1));
        var fifoFirst = new ExecutionQueueEntry(Guid.NewGuid(), Guid.NewGuid(), Priority: 1, QueuedAt: now);

        await queue.EnqueueAsync(low);
        await queue.EnqueueAsync(high);
        await queue.EnqueueAsync(fifoFirst);

        var result = await new GetExecutionQueueHandler(queue).ExecuteAsync();

        result.Select(e => e.ExecutionOrderId).Should().ContainInOrder(
            fifoFirst.ExecutionOrderId, high.ExecutionOrderId, low.ExecutionOrderId);
    }

    [Fact]
    public async Task ExecuteAsync_EmptyQueue_ReturnsEmptyList()
    {
        var queue = new InMemoryExecutionQueue();

        var result = await new GetExecutionQueueHandler(queue).ExecuteAsync();

        result.Should().BeEmpty();
    }
}
