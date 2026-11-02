using MechanicsSoftware.ExecutionService.Domain.Exceptions;

namespace MechanicsSoftware.ExecutionService.Domain.ValueObjects;

/// <summary>
/// Lifecycle of an item in the execution queue (ADR-011). <see cref="Status.Queued"/> (F4-18) and
/// <see cref="Status.InProgress"/> (F4-19, <c>ExecutionOrder.StartDiagnosis</c>) are produced
/// today; publishing <c>ExecutionCompleted</c>/<c>ExecutionFailed</c> is F4-20, and the
/// <c>Cancelled</c> compensation is F4-21. The full transition table was defined in F4-18 so
/// those issues only add behaviour, not domain shape.
/// </summary>
public sealed class ExecutionStatus(ExecutionStatus.Status value) : ValueObject
{
    public enum Status
    {
        Queued,
        InProgress,
        Completed,
        Failed,
        Cancelled
    }

    public Status Value => value;

    private static readonly HashSet<(Status From, Status To)> _validTransitions =
    [
        (Status.Queued,     Status.InProgress),
        (Status.Queued,     Status.Cancelled),
        (Status.InProgress, Status.Completed),
        (Status.InProgress, Status.Failed),
        (Status.InProgress, Status.Cancelled),
    ];

    public ExecutionStatus TransitionTo(Status newStatus)
    {
        if (!_validTransitions.Contains((Value, newStatus)))
            throw new DomainException($"Cannot transition from '{this}' to '{new ExecutionStatus(newStatus)}'.");

        return new ExecutionStatus(newStatus);
    }

    public bool Is(Status status) => Value == status;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value switch
    {
        Status.Queued => "QUEUED",
        Status.InProgress => "IN_PROGRESS",
        Status.Completed => "COMPLETED",
        Status.Failed => "FAILED",
        Status.Cancelled => "CANCELLED",
        _ => Value.ToString()
    };

    public static ExecutionStatus CreateQueued() => new(Status.Queued);
}
