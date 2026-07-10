namespace ScreenMind.Core.Models;

/// <summary>
/// A reusable analysis preset: system prompt plus generation preferences.
/// Built-in profiles ship with the app; users can add their own.
/// </summary>
public sealed record AnalysisProfile
{
    public AnalysisProfile(string id, string name, string systemPrompt, bool isBuiltIn = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(systemPrompt);
        Id = id;
        Name = name;
        SystemPrompt = systemPrompt;
        IsBuiltIn = isBuiltIn;
    }

    public string Id { get; }

    public string Name { get; }

    public string SystemPrompt { get; }

    public bool IsBuiltIn { get; }
}
