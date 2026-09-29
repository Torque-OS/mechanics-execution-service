using Microsoft.AspNetCore.Mvc;

namespace MechanicsSoftware.ExecutionService.Api.Controllers;

// Placeholder for the Execution Service's own endpoints (execution queue, diagnosis/repair
// status — F4-18/F4-19). Domain, Application and Infrastructure layers are added there,
// mirroring the layout of mechanics-software (ADR-004).
[ApiController]
[Route("api/executions")]
public class ExecutionQueueController : ControllerBase
{
    [HttpGet("{id:guid}")]
    public IActionResult Get(Guid id) =>
        StatusCode(StatusCodes.Status501NotImplemented, new { message = "Not implemented yet — see F4-18." });
}
