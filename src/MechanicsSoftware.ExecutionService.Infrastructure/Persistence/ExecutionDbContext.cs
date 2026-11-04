using System.Reflection;
using MassTransit;
using MechanicsSoftware.ExecutionService.Application.Abstractions;
using MechanicsSoftware.ExecutionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MechanicsSoftware.ExecutionService.Infrastructure.Persistence;

public sealed class ExecutionDbContext(DbContextOptions<ExecutionDbContext> options)
    : DbContext(options), IExecutionDbContext
{
    public DbSet<ExecutionOrder> ExecutionOrders { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // MassTransit transactional inbox/outbox (ADR-011), same convention as mechanics-software
        // and mechanics-billing-service.
        modelBuilder.AddInboxStateEntity(entity => entity.ToTable("inbox_state"));
        modelBuilder.AddOutboxMessageEntity(entity => entity.ToTable("outbox_message"));
        modelBuilder.AddOutboxStateEntity(entity => entity.ToTable("outbox_state"));
    }
}
