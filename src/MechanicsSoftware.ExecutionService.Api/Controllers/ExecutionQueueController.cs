using MechanicsSoftware.ExecutionService.Api.Transport;
using MechanicsSoftware.ExecutionService.Application.UseCases.Executions.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace MechanicsSoftware.ExecutionService.Api.Controllers;

// Entries arrive via the EnqueueExecution saga command (EnqueueExecutionConsumer). Staff drive
// diagnosis/repair (F4-19) through these endpoints; completion/failure (F4-20) and the
// compensation path (F4-21) are the remaining Saga-only pieces.
[ApiController]
[Route("api/executions")]
public class ExecutionQueueController(
    GetExecutionQueueHandler getQueueHandler,
    GetExecutionHandler getHandler,
    StartDiagnosisHandler startDiagnosisHandler,
    AddExecutionNoteHandler addNoteHandler,
    GetExecutionNotesHandler getNotesHandler) : ControllerBase
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
}
