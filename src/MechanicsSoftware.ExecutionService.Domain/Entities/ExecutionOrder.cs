using MechanicsSoftware.ExecutionService.Domain.Exceptions;
using MechanicsSoftware.ExecutionService.Domain.ValueObjects;

namespace MechanicsSoftware.ExecutionService.Domain.Entities;

/// <summary>
/// An entry in the execution queue, created from the <c>EnqueueExecution</c> saga command once
/// the Billing Service confirms payment (ADR-011, F4-18).
/// </summary>
public sealed class ExecutionOrder : Entity<Guid>
{
    public Guid ServiceOrderId { get; private set; }
    public ExecutionStatus Status { get; private set; } = null!;

    /// <summary>
    /// Queue priority: lower values are served first. Defaults to 0 (plain FIFO by
    /// <see cref="QueuedAt"/>) — the OS Service does not yet send a priority hint, so every
    /// order currently queues at the same priority. See README for the ordering rule.
    /// </summary>
    public int Priority { get; private set; }

    public DateTime QueuedAt { get; private set; }

    private ExecutionOrder() { }

    public static ExecutionOrder Create(Guid serviceOrderId, int priority = 0)
    {
        if (serviceOrderId == Guid.Empty)
            throw new DomainException("ServiceOrderId is required.");

        if (priority < 0)
            throw new DomainException("Priority cannot be negative.");

        return new ExecutionOrder
        {
            Id = Guid.NewGuid(),
            ServiceOrderId = serviceOrderId,
            Priority = priority,
            Status = ExecutionStatus.CreateQueued(),
            QueuedAt = DateTime.UtcNow
        };
    }
}
