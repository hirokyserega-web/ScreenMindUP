namespace ScreenMind.Core.Models;

/// <summary>Identifies a concrete model exposed by a provider.</summary>
public sealed record AiModel
{
    public AiModel(string providerId, string modelId, bool supportsVision = true, string? displayName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);
        ProviderId = providerId;
        ModelId = modelId;
        SupportsVision = supportsVision;
        DisplayName = displayName ?? modelId;
    }

    /// <summary>Stable identifier of the provider adapter, e.g. "openai" or "ollama".</summary>
    public string ProviderId { get; }

    /// <summary>Provider-specific model identifier, e.g. "gpt-4o".</summary>
    public string ModelId { get; }

    public bool SupportsVision { get; }

    public string DisplayName { get; }
}
