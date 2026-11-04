using MechanicsSoftware.ExecutionService.Application.UseCases.Executions.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace MechanicsSoftware.ExecutionService.Api.Controllers;

// Read-only for now: entries only arrive via the EnqueueExecution saga command
// (EnqueueExecutionConsumer). Staff-facing write actions (start diagnosis, etc.) are F4-19.
[ApiController]
[Route("api/executions")]
public class ExecutionQueueController(
    GetExecutionQueueHandler getQueueHandler,
    GetExecutionHandler getHandler) : ControllerBase
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
}
