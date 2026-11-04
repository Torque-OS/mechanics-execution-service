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
    public DateTime? StartedAt { get; private set; }
    public DateTime? FinishedAt { get; private set; }
    public string? FailureReason { get; private set; }

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

    /// <summary>Diagnosis/repair work begins; the order leaves the queue (F4-19).</summary>
    public void StartDiagnosis()
    {
        Status = Status.TransitionTo(ExecutionStatus.Status.InProgress);
        StartedAt = DateTime.UtcNow;
    }

    /// <summary>Repair finished successfully; publishes <c>ExecutionCompleted</c> (F4-20).</summary>
    public void Complete()
    {
        Status = Status.TransitionTo(ExecutionStatus.Status.Completed);
        FinishedAt = DateTime.UtcNow;
    }

    /// <summary>Repair could not be finished; publishes <c>ExecutionFailed</c> (F4-20) — triggers Saga compensation.</summary>
    public void Fail(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Failure reason is required.");

        Status = Status.TransitionTo(ExecutionStatus.Status.Failed);
        FinishedAt = DateTime.UtcNow;
        FailureReason = reason;
    }
}
