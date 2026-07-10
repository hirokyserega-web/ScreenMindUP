namespace ScreenMind.Core.Models;

public enum MessageRole
{
    System,
    User,
    Assistant,
}

/// <summary>A single message in the in-memory chat session.</summary>
public sealed record ChatMessage
{
    public ChatMessage(MessageRole role, string text, bool includesImage = false)
    {
        ArgumentNullException.ThrowIfNull(text);
        Role = role;
        Text = text;
        IncludesImage = includesImage;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public MessageRole Role { get; }

    public string Text { get; }

    /// <summary>True when the session screenshot was attached to this message.</summary>
    public bool IncludesImage { get; }

    public DateTimeOffset CreatedAtUtc { get; }
}
