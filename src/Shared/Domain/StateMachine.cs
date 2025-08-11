using System.Collections.Generic;

namespace Shared.Domain;

public enum ToolState
{
    IDLE,
    LOADED,
    PROCESSING,
    UNLOADING
}

public class StateMachine
{
    private static readonly Dictionary<ToolState, ToolState[]> _transitions = new()
    {
        [ToolState.IDLE] = new[] { ToolState.LOADED },
        [ToolState.LOADED] = new[] { ToolState.PROCESSING },
        [ToolState.PROCESSING] = new[] { ToolState.UNLOADING },
        [ToolState.UNLOADING] = new[] { ToolState.IDLE }
    };

    public ToolState State { get; private set; } = ToolState.IDLE;

    public bool CanTransition(ToolState target) => _transitions[State].Contains(target);

    public bool TryTransition(ToolState target)
    {
        if (CanTransition(target))
        {
            State = target;
            return true;
        }
        return false;
    }

    public void Validate(ToolState target)
    {
        if (!CanTransition(target))
            throw new InvalidOperationException($"Invalid transition from {State} to {target}");
    }
}
