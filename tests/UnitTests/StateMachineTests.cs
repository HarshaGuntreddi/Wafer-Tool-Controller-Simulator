using FluentAssertions;
using Shared.Domain;

namespace UnitTests;

public class StateMachineTests
{
    [Fact]
    public void Allows_valid_transitions()
    {
        var sm = new StateMachine();
        sm.CanTransition(ToolState.LOADED).Should().BeTrue();
        sm.TryTransition(ToolState.LOADED).Should().BeTrue();
        sm.State.Should().Be(ToolState.LOADED);
        sm.CanTransition(ToolState.PROCESSING).Should().BeTrue();
    }

    [Fact]
    public void Rejects_invalid_transition()
    {
        var sm = new StateMachine();
        Action act = () => sm.Validate(ToolState.PROCESSING);
        act.Should().Throw<InvalidOperationException>();
        sm.TryTransition(ToolState.PROCESSING).Should().BeFalse();
    }
}
