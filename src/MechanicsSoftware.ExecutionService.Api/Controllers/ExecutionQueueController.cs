using MechanicsSoftware.ExecutionService.Api.Transport;
using MechanicsSoftware.ExecutionService.Application.UseCases.Executions.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace MechanicsSoftware.ExecutionService.Api.Controllers;

// Entries arrive via the EnqueueExecution saga command (EnqueueExecutionConsumer). Staff drive
// diagnosis/repair (F4-19), completion and failure (F4-20) through these endpoints; the
// compensation path (F4-21, triggered by the OS Service) is the remaining Saga-only piece.
[ApiController]
[Route("api/executions")]
public class ExecutionQueueController(
    GetExecutionQueueHandler getQueueHandler,
    GetExecutionHandler getHandler,
    StartDiagnosisHandler startDiagnosisHandler,
    AddExecutionNoteHandler addNoteHandler,
    GetExecutionNotesHandler getNotesHandler,
    CompleteExecutionHandler completeHandler,
    FailExecutionHandler failHandler) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetQueue(CancellationToken cancellationToken)
    {
        var result = await getQueueHandler.ExecuteAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await getHandler.ExecuteAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/start-diagnosis")]
    public async Task<IActionResult> StartDiagnosis(Guid id, CancellationToken cancellationToken)
    {
        var result = await startDiagnosisHandler.ExecuteAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/notes")]
    public async Task<IActionResult> AddNote(Guid id, AddExecutionNoteRequest request, CancellationToken cancellationToken)
    {
        await addNoteHandler.ExecuteAsync(id, request.Text, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/notes")]
    public async Task<IActionResult> GetNotes(Guid id, CancellationToken cancellationToken)
    {
        var result = await getNotesHandler.ExecuteAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<IActionResult> Complete(Guid id, CancellationToken cancellationToken)
    {
        var result = await completeHandler.ExecuteAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/fail")]
    public async Task<IActionResult> Fail(Guid id, FailExecutionRequest request, CancellationToken cancellationToken)
    {
        var result = await failHandler.ExecuteAsync(id, request.Reason, cancellationToken);
        return Ok(result);
    }
}
