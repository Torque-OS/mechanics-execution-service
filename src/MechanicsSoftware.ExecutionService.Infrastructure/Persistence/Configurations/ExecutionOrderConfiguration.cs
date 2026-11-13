using MechanicsSoftware.ExecutionService.Domain.Entities;
using MechanicsSoftware.ExecutionService.Domain.Exceptions;
using MechanicsSoftware.ExecutionService.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MechanicsSoftware.ExecutionService.Infrastructure.Persistence.Configurations;

public sealed class ExecutionOrderConfiguration : IEntityTypeConfiguration<ExecutionOrder>
{
    public void Configure(EntityTypeBuilder<ExecutionOrder> builder)
    {
        builder.ToTable("execution_orders");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.ServiceOrderId).HasColumnName("service_order_id").IsRequired();

        // One execution order per service order — also enforces the idempotency in
        // EnqueueExecutionHandler at the database level.
        builder.HasIndex(e => e.ServiceOrderId).IsUnique().HasDatabaseName("ix_execution_orders_service_order_id");

        builder.Property(e => e.Status)
            .HasConversion(v => v.ToString(), v => ParseStatus(v))
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.Priority).HasColumnName("priority").IsRequired();
        builder.Property(e => e.QueuedAt).HasColumnName("queued_at").IsRequired();
        builder.Property(e => e.StartedAt).HasColumnName("started_at");
        builder.Property(e => e.FinishedAt).HasColumnName("finished_at");
        builder.Property(e => e.FailureReason).HasColumnName("failure_reason").HasMaxLength(500);
    }

    private static readonly Dictionary<string, ExecutionStatus.Status> _statusMap = new()
    {
        ["QUEUED"] = ExecutionStatus.Status.Queued,
        ["IN_PROGRESS"] = ExecutionStatus.Status.InProgress,
        ["COMPLETED"] = ExecutionStatus.Status.Completed,
        ["FAILED"] = ExecutionStatus.Status.Failed,
        ["CANCELLED"] = ExecutionStatus.Status.Cancelled,
    };

    internal static ExecutionStatus ParseStatus(string value) =>
        _statusMap.TryGetValue(value, out var status)
            ? new ExecutionStatus(status)
            : throw new DomainException($"Unknown execution status: '{value}'");
}
