using System.Diagnostics.CodeAnalysis;
using MechanicsSoftware.ExecutionService.Application.UseCases.Executions.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace MechanicsSoftware.ExecutionService.Application;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<EnqueueExecutionHandler>();
        services.AddScoped<GetExecutionQueueHandler>();
        services.AddScoped<GetExecutionHandler>();
        services.AddScoped<StartDiagnosisHandler>();
        services.AddScoped<AddExecutionNoteHandler>();
        services.AddScoped<GetExecutionNotesHandler>();
        services.AddScoped<CompleteExecutionHandler>();
        services.AddScoped<FailExecutionHandler>();
        services.AddScoped<CancelExecutionHandler>();

        return services;
    }
}
