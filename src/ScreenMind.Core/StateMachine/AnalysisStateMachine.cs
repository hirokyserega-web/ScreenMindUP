namespace ScreenMind.Core.StateMachine;

/// <summary>Notification raised after a successful state change.</summary>
public sealed record AnalysisStateChangedEventArgs(AnalysisState From, AnalysisState To);

/// <summary>
/// Thread-safe state machine guarding the primary analysis pipeline.
/// Enforces valid transitions and the single-active-analysis invariant:
/// a new analysis can only start from Idle, Completed or Failed.
/// </summary>
public sealed class AnalysisStateMachine
{
    private static readonly IReadOnlyDictionary<AnalysisState, AnalysisState[]> _transitions =
        new Dictionary<AnalysisState, AnalysisState[]>
        {
            [AnalysisState.Idle] = [AnalysisState.Selecting, AnalysisState.Capturing],
            [AnalysisState.Selecting] = [AnalysisState.Capturing, AnalysisState.Idle, AnalysisState.Cancelling, AnalysisState.Failed],
            [AnalysisState.Capturing] = [AnalysisState.Preprocessing, AnalysisState.Cancelling, AnalysisState.Failed],
            [AnalysisState.Preprocessing] = [AnalysisState.Sending, AnalysisState.Cancelling, AnalysisState.Failed],
            [AnalysisState.Sending] = [AnalysisState.Streaming, AnalysisState.Completed, AnalysisState.Cancelling, AnalysisState.Failed],
            [AnalysisState.Streaming] = [AnalysisState.Completed, AnalysisState.Cancelling, AnalysisState.Failed],
            [AnalysisState.Completed] = [AnalysisState.Idle, AnalysisState.Selecting, AnalysisState.Capturing],
            [AnalysisState.Cancelling] = [AnalysisState.Idle],
            [AnalysisState.Failed] = [AnalysisState.Idle, AnalysisState.Selecting, AnalysisState.Capturing],
        };

    private readonly object _gate = new();
    private AnalysisState _state = AnalysisState.Idle;

    public event EventHandler<AnalysisStateChangedEventArgs>? StateChanged;

    public AnalysisState State
    {
        get
        {
            lock (_gate)
            {
                return _state;
            }
        }
    }

    /// <summary>True when a new primary analysis may begin.</summary>
    public bool CanStartAnalysis
    {
        get
        {
            lock (_gate)
            {
                return _state is AnalysisState.Idle or AnalysisState.Completed or AnalysisState.Failed;
            }
        }
    }

    /// <summary>Performs a transition or throws <see cref="InvalidStateTransitionException"/>.</summary>
    public void TransitionTo(AnalysisState next)
    {
        AnalysisState previous;
        lock (_gate)
        {
            if (!IsAllowed(_state, next))
            {
                throw new InvalidStateTransitionException(_state, next);
            }

            previous = _state;
            _state = next;
        }

        StateChanged?.Invoke(this, new AnalysisStateChangedEventArgs(previous, next));
    }

    /// <summary>Attempts a transition without throwing. Returns false when disallowed.</summary>
    public bool TryTransitionTo(AnalysisState next)
    {
        AnalysisState previous;
        lock (_gate)
        {
            if (!IsAllowed(_state, next))
            {
                return false;
            }

            previous = _state;
            _state = next;
        }

        StateChanged?.Invoke(this, new AnalysisStateChangedEventArgs(previous, next));
        return true;
    }

    /// <summary>
    /// Attempts to begin a new analysis (moving to the given entry state).
    /// Fails when another analysis is active, enforcing the single-request invariant.
    /// </summary>
    public bool TryBeginAnalysis(AnalysisState entryState)
    {
        if (entryState is not (AnalysisState.Selecting or AnalysisState.Capturing))
        {
            throw new ArgumentOutOfRangeException(
                nameof(entryState),
                "An analysis starts in Selecting or Capturing.");
        }

        AnalysisState previous;
        lock (_gate)
        {
            if (_state is not (AnalysisState.Idle or AnalysisState.Completed or AnalysisState.Failed))
            {
                return false;
            }

            previous = _state;
            _state = entryState;
        }

        StateChanged?.Invoke(this, new AnalysisStateChangedEventArgs(previous, entryState));
        return true;
    }

    private static bool IsAllowed(AnalysisState from, AnalysisState to) =>
        from != to && _transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
}
