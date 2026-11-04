using System.Diagnostics.CodeAnalysis;
using MechanicsSoftware.ExecutionService.Api.Middleware;
using MechanicsSoftware.ExecutionService.Application;
using MechanicsSoftware.ExecutionService.Infrastructure;
using MechanicsSoftware.ExecutionService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Migrations run on startup, as in mechanics-software and mechanics-billing-service (ADR-006).
// Skipped for non-relational providers (the EF InMemory provider used by unit tests).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ExecutionDbContext>();
    if (db.Database.IsRelational())
        await db.Database.MigrateAsync();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

// Liveness/readiness probe for Kubernetes (see k8s/deployment.yaml).
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "execution-service" }));

app.UseAuthorization();

app.MapControllers();

await app.RunAsync();

// Exposed for WebApplicationFactory-based integration tests.
[ExcludeFromCodeCoverage]
public partial class Program
{
    protected Program() { }
}
