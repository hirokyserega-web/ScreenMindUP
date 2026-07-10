namespace ScreenMind.Core.Models;

/// <summary>Provider-agnostic streaming events emitted while a response is generated.</summary>
public abstract record AiStreamEvent
{
    private AiStreamEvent()
    {
    }

    /// <summary>An incremental chunk of response text.</summary>
    public sealed record TextDelta(string Text) : AiStreamEvent;

    /// <summary>The stream finished successfully.</summary>
    public sealed record Completed(TokenUsage Usage) : AiStreamEvent;

    /// <summary>The stream terminated with a normalized error.</summary>
    public sealed record Failed(AiError Error) : AiStreamEvent;
}
