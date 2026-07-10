namespace ScreenMind.Core.StateMachine;

/// <summary>Lifecycle states of the primary analysis pipeline.</summary>
public enum AnalysisState
{
    Idle,
    Selecting,
    Capturing,
    Preprocessing,
    Sending,
    Streaming,
    Completed,
    Cancelling,
    Failed,
}
