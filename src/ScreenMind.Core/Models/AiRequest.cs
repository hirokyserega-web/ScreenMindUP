namespace ScreenMind.Core.Models;

/// <summary>
/// A fully assembled multimodal request: profile-driven system prompt, optional
/// screenshot, prior conversation and the current user question.
/// </summary>
public sealed record AiRequest
{
    public AiRequest(
        AiModel model,
        string systemPrompt,
        IReadOnlyList<ChatMessage> messages,
        CapturedImage? image,
        TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(systemPrompt);
        ArgumentNullException.ThrowIfNull(messages);
        if (messages.Count == 0)
        {
            throw new ArgumentException("A request must contain at least one message.", nameof(messages));
        }

        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be positive.");
        }

        if (image is { IsDisposed: true })
        {
            throw new ArgumentException("Cannot send a disposed image.", nameof(image));
        }

        Model = model;
        SystemPrompt = systemPrompt;
        Messages = messages;
        Image = image;
        Timeout = timeout;
    }

    public AiModel Model { get; }

    public string SystemPrompt { get; }

    public IReadOnlyList<ChatMessage> Messages { get; }

    public CapturedImage? Image { get; }

    public TimeSpan Timeout { get; }
}
