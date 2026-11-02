using FluentAssertions;
using MechanicsSoftware.ExecutionService.Domain.Exceptions;
using MechanicsSoftware.ExecutionService.Domain.ValueObjects;

namespace MechanicsSoftware.ExecutionService.UnitTests.Domain;

public class ExecutionStatusTests
{
    [Fact]
    public void CreateQueued_ReturnsQueuedStatus()
    {
        var status = ExecutionStatus.CreateQueued();
        status.Value.Should().Be(ExecutionStatus.Status.Queued);
    }

    [Theory]
    [InlineData(ExecutionStatus.Status.Queued, ExecutionStatus.Status.InProgress)]
    [InlineData(ExecutionStatus.Status.Queued, ExecutionStatus.Status.Cancelled)]
    [InlineData(ExecutionStatus.Status.InProgress, ExecutionStatus.Status.Completed)]
    [InlineData(ExecutionStatus.Status.InProgress, ExecutionStatus.Status.Failed)]
    [InlineData(ExecutionStatus.Status.InProgress, ExecutionStatus.Status.Cancelled)]
    public void TransitionTo_ValidTransition_Succeeds(ExecutionStatus.Status from, ExecutionStatus.Status to)
    {
        var status = new ExecutionStatus(from);
        var result = status.TransitionTo(to);
        result.Value.Should().Be(to);
    }

    [Theory]
    [InlineData(ExecutionStatus.Status.Queued, ExecutionStatus.Status.Completed)]
    [InlineData(ExecutionStatus.Status.Completed, ExecutionStatus.Status.InProgress)]
    [InlineData(ExecutionStatus.Status.Cancelled, ExecutionStatus.Status.InProgress)]
    [InlineData(ExecutionStatus.Status.Failed, ExecutionStatus.Status.Completed)]
    public void TransitionTo_InvalidTransition_Throws(ExecutionStatus.Status from, ExecutionStatus.Status to)
    {
        var status = new ExecutionStatus(from);
        var act = () => status.TransitionTo(to);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ToString_AllStatuses_ReturnUpperSnakeCase()
    {
        new ExecutionStatus(ExecutionStatus.Status.Queued).ToString().Should().Be("QUEUED");
        new ExecutionStatus(ExecutionStatus.Status.InProgress).ToString().Should().Be("IN_PROGRESS");
        new ExecutionStatus(ExecutionStatus.Status.Completed).ToString().Should().Be("COMPLETED");
        new ExecutionStatus(ExecutionStatus.Status.Failed).ToString().Should().Be("FAILED");
        new ExecutionStatus(ExecutionStatus.Status.Cancelled).ToString().Should().Be("CANCELLED");
    }

    [Fact]
    public void Is_MatchingStatus_ReturnsTrue()
    {
        var status = ExecutionStatus.CreateQueued();
        status.Is(ExecutionStatus.Status.Queued).Should().BeTrue();
        status.Is(ExecutionStatus.Status.Completed).Should().BeFalse();
    }

    [Fact]
    public void Equality_SameStatus_AreEqual()
    {
        var a = ExecutionStatus.CreateQueued();
        var b = ExecutionStatus.CreateQueued();
        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }
}
