namespace MechanicsSoftware.ExecutionService.Application.Exceptions;

public sealed class NotFoundException(string entityName, object key)
    : Exception($"{entityName} '{key}' was not found.");
