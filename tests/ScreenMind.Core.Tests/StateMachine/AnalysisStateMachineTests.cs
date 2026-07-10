using ScreenMind.Core.StateMachine;

namespace ScreenMind.Core.Tests.StateMachine;

public sealed class AnalysisStateMachineTests
{
    [Fact]
    public void Starts_in_idle()
    {
        var sm = new AnalysisStateMachine();

        sm.State.Should().Be(AnalysisState.Idle);
        sm.CanStartAnalysis.Should().BeTrue();
    }

    [Theory]
    [InlineData(AnalysisState.Selecting)]
    [InlineData(AnalysisState.Capturing)]
    public void Analysis_can_begin_from_idle(AnalysisState entry)
    {
        var sm = new AnalysisStateMachine();

        sm.TryBeginAnalysis(entry).Should().BeTrue();
        sm.State.Should().Be(entry);
    }

    [Fact]
    public void Happy_path_reaches_completed()
    {
        var sm = new AnalysisStateMachine();

        sm.TransitionTo(AnalysisState.Selecting);
        sm.TransitionTo(AnalysisState.Capturing);
        sm.TransitionTo(AnalysisState.Preprocessing);
        sm.TransitionTo(AnalysisState.Sending);
        sm.TransitionTo(AnalysisState.Streaming);
        sm.TransitionTo(AnalysisState.Completed);

        sm.State.Should().Be(AnalysisState.Completed);
        sm.CanStartAnalysis.Should().BeTrue();
    }

    [Fact]
    public void Non_streaming_response_can_complete_directly_from_sending()
    {
        var sm = BuildInState(AnalysisState.Sending);

        sm.TransitionTo(AnalysisState.Completed);

        sm.State.Should().Be(AnalysisState.Completed);
        sm.CanStartAnalysis.Should().BeTrue();
    }

    [Fact]
    public void Selection_failure_moves_analysis_to_failed()
    {
        var sm = BuildInState(AnalysisState.Selecting);

        sm.TransitionTo(AnalysisState.Failed);

        sm.State.Should().Be(AnalysisState.Failed);
        sm.CanStartAnalysis.Should().BeTrue();
    }

    [Theory]
    [InlineData(AnalysisState.Idle, AnalysisState.Streaming)]
    [InlineData(AnalysisState.Idle, AnalysisState.Completed)]
    [InlineData(AnalysisState.Idle, AnalysisState.Failed)]
    [InlineData(AnalysisState.Idle, AnalysisState.Cancelling)]
    public void Invalid_transitions_from_idle_throw(AnalysisState from, AnalysisState to)
    {
        var sm = new AnalysisStateMachine();
        sm.State.Should().Be(from);

        var act = () => sm.TransitionTo(to);

        act.Should().Throw<InvalidStateTransitionException>()
            .Which.Should().Match<InvalidStateTransitionException>(e => e.From == from && e.To == to);
    }

    [Fact]
    public void Self_transition_is_rejected()
    {
        var sm = new AnalysisStateMachine();

        var act = () => sm.TransitionTo(AnalysisState.Idle);

        act.Should().Throw<InvalidStateTransitionException>();
    }

    [Fact]
    public void Parallel_analysis_is_rejected_while_active()
    {
        var sm = new AnalysisStateMachine();
        sm.TryBeginAnalysis(AnalysisState.Capturing).Should().BeTrue();

        sm.TryBeginAnalysis(AnalysisState.Capturing).Should().BeFalse();
        sm.CanStartAnalysis.Should().BeFalse();
        sm.State.Should().Be(AnalysisState.Capturing);
    }

    [Theory]
    [InlineData(AnalysisState.Capturing)]
    [InlineData(AnalysisState.Preprocessing)]
    [InlineData(AnalysisState.Sending)]
    [InlineData(AnalysisState.Streaming)]
    public void Cancellation_is_reachable_from_every_active_state(AnalysisState active)
    {
        var sm = BuildInState(active);

        sm.TransitionTo(AnalysisState.Cancelling);
        sm.TransitionTo(AnalysisState.Idle);

        sm.State.Should().Be(AnalysisState.Idle);
        sm.CanStartAnalysis.Should().BeTrue();
    }

    [Theory]
    [InlineData(AnalysisState.Capturing)]
    [InlineData(AnalysisState.Preprocessing)]
    [InlineData(AnalysisState.Sending)]
    [InlineData(AnalysisState.Streaming)]
    public void Failure_is_reachable_from_every_active_state(AnalysisState active)
    {
        var sm = BuildInState(active);

        sm.TransitionTo(AnalysisState.Failed);

        sm.CanStartAnalysis.Should().BeTrue();
    }

    [Fact]
    public void Cancelling_only_returns_to_idle()
    {
        var sm = BuildInState(AnalysisState.Streaming);
        sm.TransitionTo(AnalysisState.Cancelling);

        sm.TryTransitionTo(AnalysisState.Completed).Should().BeFalse();
        sm.TryTransitionTo(AnalysisState.Streaming).Should().BeFalse();
        sm.TryTransitionTo(AnalysisState.Idle).Should().BeTrue();
    }

    [Fact]
    public void New_analysis_can_start_after_failure()
    {
        var sm = BuildInState(AnalysisState.Capturing);
        sm.TransitionTo(AnalysisState.Failed);

        sm.TryBeginAnalysis(AnalysisState.Selecting).Should().BeTrue();
    }

    [Fact]
    public void Begin_analysis_rejects_non_entry_states()
    {
        var sm = new AnalysisStateMachine();

        var act = () => sm.TryBeginAnalysis(AnalysisState.Streaming);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void State_changed_event_reports_previous_and_next()
    {
        var sm = new AnalysisStateMachine();
        AnalysisStateChangedEventArgs? observed = null;
        sm.StateChanged += (_, e) => observed = e;

        sm.TransitionTo(AnalysisState.Capturing);

        observed.Should().NotBeNull();
        observed!.From.Should().Be(AnalysisState.Idle);
        observed.To.Should().Be(AnalysisState.Capturing);
    }

    [Fact]
    public async Task Concurrent_begin_attempts_admit_exactly_one()
    {
        var sm = new AnalysisStateMachine();
        var attempts = Enumerable.Range(0, 32)
            .Select(_ => Task.Run(() => sm.TryBeginAnalysis(AnalysisState.Capturing)));

        var results = await Task.WhenAll(attempts);

        results.Count(r => r).Should().Be(1);
        sm.State.Should().Be(AnalysisState.Capturing);
    }

    private static AnalysisStateMachine BuildInState(AnalysisState target)
    {
        var sm = new AnalysisStateMachine();
        foreach (var step in Path(target))
        {
            sm.TransitionTo(step);
        }

        return sm;
    }

    private static IEnumerable<AnalysisState> Path(AnalysisState target)
    {
        AnalysisState[] chain =
        [
            AnalysisState.Capturing,
            AnalysisState.Preprocessing,
            AnalysisState.Sending,
            AnalysisState.Streaming,
        ];
        foreach (var state in chain)
        {
            yield return state;
            if (state == target)
            {
                yield break;
            }
        }
    }
}
