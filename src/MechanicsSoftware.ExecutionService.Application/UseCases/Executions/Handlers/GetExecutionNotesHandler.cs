using MechanicsSoftware.ExecutionService.Application.Abstractions;

namespace MechanicsSoftware.ExecutionService.Application.UseCases.Executions.Handlers;

public sealed class GetExecutionNotesHandler(IExecutionNotes notes)
{
    public async Task<IReadOnlyList<ExecutionNoteResponse>> ExecuteAsync(
        Guid executionOrderId, CancellationToken cancellationToken = default)
    {
        var result = await notes.ListAsync(executionOrderId, cancellationToken);
        return result.Select(ExecutionNoteResponse.From).ToList();
    }
}

public sealed record ExecutionNoteResponse(string Text, DateTime RecordedAt)
{
    public static ExecutionNoteResponse From(ExecutionNote note) => new(note.Text, note.RecordedAt);
}
