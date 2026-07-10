namespace ScreenMind.Core.Models;

/// <summary>Token accounting reported by a provider for a single exchange.</summary>
public sealed record TokenUsage(int InputTokens, int OutputTokens)
{
    public static TokenUsage Empty { get; } = new(0, 0);

    public int TotalTokens => InputTokens + OutputTokens;
}
