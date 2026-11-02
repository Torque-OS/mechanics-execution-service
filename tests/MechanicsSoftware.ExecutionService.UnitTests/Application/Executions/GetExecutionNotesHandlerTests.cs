using FluentAssertions;
using MechanicsSoftware.ExecutionService.Application.Abstractions;
using MechanicsSoftware.ExecutionService.Application.UseCases.Executions.Handlers;
using MechanicsSoftware.ExecutionService.UnitTests.Helpers;

namespace MechanicsSoftware.ExecutionService.UnitTests.Application.Executions;

public class GetExecutionNotesHandlerTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsNotesForOrder()
    {
        var notes = new InMemoryExecutionNotes();
        var orderId = Guid.NewGuid();
        await notes.AddAsync(new ExecutionNote(orderId, "Diagnosis started", DateTime.UtcNow));
        await notes.AddAsync(new ExecutionNote(Guid.NewGuid(), "Other order", DateTime.UtcNow));

        var result = await new GetExecutionNotesHandler(notes).ExecuteAsync(orderId);

        result.Should().ContainSingle(n => n.Text == "Diagnosis started");
    }
}
