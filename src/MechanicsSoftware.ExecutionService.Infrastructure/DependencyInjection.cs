using System.Diagnostics.CodeAnalysis;
using MassTransit;
using MechanicsSoftware.ExecutionService.Application.Abstractions;
using MechanicsSoftware.ExecutionService.Infrastructure.Messaging;
using MechanicsSoftware.ExecutionService.Infrastructure.Persistence;
using MechanicsSoftware.ExecutionService.Infrastructure.Queue;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace MechanicsSoftware.ExecutionService.Infrastructure;

[ExcludeFromCodeCoverage]
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Connection string not found. Set 'ConnectionStrings:DefaultConnection'.");

        var mongoConnectionString = configuration.GetConnectionString("Mongo");
        if (string.IsNullOrWhiteSpace(mongoConnectionString))
            throw new InvalidOperationException(
                "Connection string not found. Set 'ConnectionStrings:Mongo'.");

        var rabbitMqConnectionString = configuration["RABBITMQ_CONNECTION_STRING"];
        if (string.IsNullOrWhiteSpace(rabbitMqConnectionString))
            throw new InvalidOperationException(
                "RabbitMQ not configured. Set 'RABBITMQ_CONNECTION_STRING' (amqp://user:password@host:5672/).");

        services.AddDbContext<ExecutionDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IExecutionDbContext>(sp => sp.GetRequiredService<ExecutionDbContext>());

        // Saga messaging (ADR-011): MassTransit v8 over RabbitMQ with the EF Core transactional
        // outbox/inbox, same convention as mechanics-software and mechanics-billing-service.
        // Contracts: TorqueOS.Contracts.Execution.
        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<EnqueueExecutionConsumer, EnqueueExecutionConsumerDefinition>();

            bus.AddEntityFrameworkOutbox<ExecutionDbContext>(outbox =>
            {
                outbox.UsePostgres();
                outbox.UseBusOutbox();
            });

            bus.UsingRabbitMq((context, rabbit) =>
            {
                rabbit.Host(new Uri(rabbitMqConnectionString));
                rabbit.ConfigureEndpoints(context);
            });
        });

        // MongoDB (ADR-012): read-optimized execution queue projection.
        var mongoUrl = MongoUrl.Create(mongoConnectionString);
        services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoUrl));
        services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoUrl.DatabaseName ?? "execution"));
        services.AddScoped<IExecutionQueue, MongoExecutionQueue>();
        services.AddScoped<IExecutionNotes, MongoExecutionNotes>();

        return services;
    }
}
