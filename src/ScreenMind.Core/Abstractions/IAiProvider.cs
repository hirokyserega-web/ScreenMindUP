using ScreenMind.Core.Models;

namespace ScreenMind.Core.Abstractions;

/// <summary>
/// Uniform contract implemented by every AI provider adapter
/// (OpenAI, Anthropic, Gemini, OpenAI-compatible, Ollama).
/// </summary>
public interface IAiProvider
{
    /// <summary>Stable identifier, e.g. "openai", "anthropic", "gemini", "openai-compatible", "ollama".</summary>
    string ProviderId { get; }

    /// <summary>Human-readable provider name.</summary>
    string DisplayName { get; }

    /// <summary>
    /// Streams a multimodal completion. Implementations must honor cancellation
    /// promptly, preserve chunk order and terminate with exactly one
    /// <see cref="AiStreamEvent.Completed"/> or <see cref="AiStreamEvent.Failed"/>.
    /// </summary>
    IAsyncEnumerable<AiStreamEvent> StreamAsync(AiRequest request, CancellationToken cancellationToken);

    /// <summary>Lightweight connectivity/credentials check. Never throws for expected failures.</summary>
    Task<AiResult> TestConnectionAsync(CancellationToken cancellationToken);

    /// <summary>Models the provider can offer; may be a static list or a live query.</summary>
    Task<IReadOnlyList<AiModel>> ListModelsAsync(CancellationToken cancellationToken);
}
