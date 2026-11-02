using FluentAssertions;
using MechanicsSoftware.ExecutionService.Domain.Entities;
using MechanicsSoftware.ExecutionService.Domain.Exceptions;
using MechanicsSoftware.ExecutionService.Domain.ValueObjects;

namespace MechanicsSoftware.ExecutionService.UnitTests.Domain;

public class ExecutionOrderTests
{
    [Fact]
    public void Create_ValidArgs_StatusIsQueued()
    {
        var order = ExecutionOrder.Create(Guid.NewGuid());
        order.Status.Value.Should().Be(ExecutionStatus.Status.Queued);
    }

    [Fact]
    public void Create_ValidArgs_SetsQueuedAt()
    {
        var before = DateTime.UtcNow;
        var order = ExecutionOrder.Create(Guid.NewGuid());
        order.QueuedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Create_NoPriority_DefaultsToZero()
    {
        var order = ExecutionOrder.Create(Guid.NewGuid());
        order.Priority.Should().Be(0);
    }

    [Fact]
    public void Create_WithPriority_SetsPriority()
    {
        var order = ExecutionOrder.Create(Guid.NewGuid(), priority: 5);
        order.Priority.Should().Be(5);
    }

    [Fact]
    public void Create_EmptyServiceOrderId_Throws()
    {
        var act = () => ExecutionOrder.Create(Guid.Empty);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_NegativePriority_Throws()
    {
        var act = () => ExecutionOrder.Create(Guid.NewGuid(), priority: -1);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void StartDiagnosis_FromQueued_StatusIsInProgress()
    {
        var order = ExecutionOrder.Create(Guid.NewGuid());
        order.StartDiagnosis();
        order.Status.Value.Should().Be(ExecutionStatus.Status.InProgress);
    }

    [Fact]
    public void StartDiagnosis_FromQueued_SetsStartedAt()
    {
        var order = ExecutionOrder.Create(Guid.NewGuid());
        var before = DateTime.UtcNow;
        order.StartDiagnosis();
        order.StartedAt.Should().NotBeNull();
        order.StartedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void StartDiagnosis_AlreadyInProgress_Throws()
    {
        var order = ExecutionOrder.Create(Guid.NewGuid());
        order.StartDiagnosis();
        var act = () => order.StartDiagnosis();
        act.Should().Throw<DomainException>();
    }
}
