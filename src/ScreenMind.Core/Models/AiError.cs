namespace ScreenMind.Core.Models;

/// <summary>Normalized error categories shared by every provider adapter.</summary>
public enum AiErrorKind
{
    Unknown,
    Authentication,
    RateLimited,
    Network,
    Timeout,
    UnsupportedModel,
    PayloadTooLarge,
    Cancelled,
    InvalidConfiguration,
    ContentBlocked,
    ServiceUnavailable,
}

/// <summary>
/// A normalized provider failure. Messages must never contain screen content,
/// prompts, responses or secrets.
/// </summary>
public sealed record AiError
{
    public AiError(AiErrorKind kind, string message, bool isTransient = false, Exception? cause = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        Kind = kind;
        Message = message;
        IsTransient = isTransient;
        Cause = cause;
    }

    public AiErrorKind Kind { get; }

    /// <summary>Safe, user-presentable description. No sensitive payload data.</summary>
    public string Message { get; }

    /// <summary>Transient errors are eligible for one retry / provider fallback.</summary>
    public bool IsTransient { get; }

    public Exception? Cause { get; }

    public static AiError Cancelled() =>
        new(AiErrorKind.Cancelled, "The request was cancelled.");

    public static AiError Timeout() =>
        new(AiErrorKind.Timeout, "The provider did not respond in time.", isTransient: true);
}
