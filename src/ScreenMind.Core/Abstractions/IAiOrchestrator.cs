using ScreenMind.Core.Models;

namespace ScreenMind.Core.Abstractions;

/// <summary>
/// Coordinates a single analysis pipeline: request assembly, provider routing,
/// retry/fallback policy and stream normalization. At most one primary analysis
/// may run at a time.
/// </summary>
public interface IAiOrchestrator
{
    /// <summary>
    /// Executes the request against the configured provider chain, yielding
    /// normalized stream events. Enforces the single-active-request invariant.
    /// </summary>
    IAsyncEnumerable<AiStreamEvent> ExecuteAsync(AiRequest request, CancellationToken cancellationToken);

    /// <summary>True while a primary analysis is in flight.</summary>
    bool IsBusy { get; }
}
