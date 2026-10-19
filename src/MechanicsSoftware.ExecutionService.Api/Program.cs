var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Liveness/readiness probe for Kubernetes (see k8s/deployment.yaml).
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "execution-service" }));

app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposed for WebApplicationFactory-based integration tests (added alongside the domain in F4-18+).
public partial class Program { }
