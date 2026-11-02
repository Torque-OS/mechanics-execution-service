using FluentAssertions;
using MechanicsSoftware.ExecutionService.Application.Exceptions;
using MechanicsSoftware.ExecutionService.Application.UseCases.Executions.Handlers;
using MechanicsSoftware.ExecutionService.Domain.Entities;
using MechanicsSoftware.ExecutionService.Domain.Exceptions;
using MechanicsSoftware.ExecutionService.UnitTests.Helpers;

namespace MechanicsSoftware.ExecutionService.UnitTests.Application.Executions;

public class AddExecutionNoteHandlerTests
{
    private static ExecutionOrder InProgressOrder()
    {
        var order = ExecutionOrder.Create(Guid.NewGuid());
        order.StartDiagnosis();
        return order;
    }

    [Fact]
    public async Task ExecuteAsync_InProgressOrder_AddsNote()
    {
        await using var db = InMemoryDbContextHelper.Create();
        var order = InProgressOrder();
        db.ExecutionOrders.Add(order);
        await db.SaveChangesAsync();
        var notes = new InMemoryExecutionNotes();

        await new AddExecutionNoteHandler(db, notes).ExecuteAsync(order.Id, "Brake pads worn, replacing.");

        var result = await notes.ListAsync(order.Id);
        result.Should().ContainSingle(n => n.Text == "Brake pads worn, replacing.");
    }

    [Fact]
    public async Task ExecuteAsync_StillQueued_ThrowsDomainException()
    {
        await using var db = InMemoryDbContextHelper.Create();
        var order = ExecutionOrder.Create(Guid.NewGuid());
        db.ExecutionOrders.Add(order);
        await db.SaveChangesAsync();
        var notes = new InMemoryExecutionNotes();

        var act = async () => await new AddExecutionNoteHandler(db, notes).ExecuteAsync(order.Id, "too early");

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task ExecuteAsync_EmptyText_ThrowsDomainException()
    {
        await using var db = InMemoryDbContextHelper.Create();
        var order = InProgressOrder();
        db.ExecutionOrders.Add(order);
        await db.SaveChangesAsync();
        var notes = new InMemoryExecutionNotes();

        var act = async () => await new AddExecutionNoteHandler(db, notes).ExecuteAsync(order.Id, "   ");

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task ExecuteAsync_UnknownId_ThrowsNotFoundException()
    {
        await using var db = InMemoryDbContextHelper.Create();
        var notes = new InMemoryExecutionNotes();

        var act = async () => await new AddExecutionNoteHandler(db, notes).ExecuteAsync(Guid.NewGuid(), "note");

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
