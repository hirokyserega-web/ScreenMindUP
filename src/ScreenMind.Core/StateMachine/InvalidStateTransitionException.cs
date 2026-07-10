namespace ScreenMind.Core.StateMachine;

/// <summary>Thrown when a transition violates the analysis state machine.</summary>
public sealed class InvalidStateTransitionException : InvalidOperationException
{
    public InvalidStateTransitionException(AnalysisState from, AnalysisState to)
        : base($"Transition from {from} to {to} is not allowed.")
    {
        From = from;
        To = to;
    }

    public AnalysisState From { get; }

    public AnalysisState To { get; }
}
