using MechanicsSoftware.ExecutionService.Application.Abstractions;
using MechanicsSoftware.ExecutionService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace MechanicsSoftware.ExecutionService.UnitTests.Helpers;

/// <summary>
/// Hosts the real API pipeline (middleware, controllers) over EF InMemory and an in-memory queue —
/// no PostgreSQL, MongoDB or RabbitMQ needed.
/// </summary>
public sealed class ExecutionApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    public InMemoryExecutionQueue Queue { get; } = new();
    public InMemoryExecutionNotes Notes { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=unused-in-tests");
        builder.UseSetting("ConnectionStrings:Mongo", "mongodb://unused-in-tests:27017/execution");
        builder.UseSetting("RABBITMQ_CONNECTION_STRING", "amqp://guest:guest@unused-in-tests:5672/");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ExecutionDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ExecutionDbContext>>();
            services.AddDbContext<ExecutionDbContext>(options => options.UseInMemoryDatabase(_databaseName));

            services.RemoveAll<IExecutionQueue>();
            services.AddSingleton<IExecutionQueue>(Queue);

            services.RemoveAll<IExecutionNotes>();
            services.AddSingleton<IExecutionNotes>(Notes);

            // No broker or PostgreSQL here: drop MassTransit's bus and outbox-delivery hosted services.
            // The consumer itself is covered by EnqueueExecutionConsumerTests (test harness).
            services.RemoveAll<IHostedService>();
        });
    }
}
