namespace ScreenMind.Core.Models;

/// <summary>Final outcome of one analysis exchange.</summary>
public sealed record AiResult
{
    private AiResult(bool isSuccess, string? text, TokenUsage usage, AiError? error)
    {
        IsSuccess = isSuccess;
        Text = text;
        Usage = usage;
        Error = error;
    }

    public bool IsSuccess { get; }

    public string? Text { get; }

    public TokenUsage Usage { get; }

    public AiError? Error { get; }

    public static AiResult Success(string text, TokenUsage usage)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(usage);
        return new AiResult(true, text, usage, null);
    }

    public static AiResult Failure(AiError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new AiResult(false, null, TokenUsage.Empty, error);
    }
}
