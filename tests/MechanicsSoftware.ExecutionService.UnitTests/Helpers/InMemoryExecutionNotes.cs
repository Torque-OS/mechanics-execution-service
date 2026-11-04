using MechanicsSoftware.ExecutionService.Application.Abstractions;

namespace MechanicsSoftware.ExecutionService.UnitTests.Helpers;

public sealed class InMemoryExecutionNotes : IExecutionNotes
{
    private readonly List<ExecutionNote> _notes = [];

    public Task AddAsync(ExecutionNote note, CancellationToken cancellationToken = default)
    {
        _notes.Add(note);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ExecutionNote>> ListAsync(Guid executionOrderId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<ExecutionNote> result = _notes.Where(n => n.ExecutionOrderId == executionOrderId).ToList();
        return Task.FromResult(result);
    }
}
