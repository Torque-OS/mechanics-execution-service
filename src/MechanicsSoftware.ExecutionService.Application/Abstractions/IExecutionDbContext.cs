using MechanicsSoftware.ExecutionService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MechanicsSoftware.ExecutionService.Application.Abstractions;

/// <summary>EF Core seam (ADR-004) — the relational store of record for execution orders.</summary>
public interface IExecutionDbContext
{
    DbSet<ExecutionOrder> ExecutionOrders { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
