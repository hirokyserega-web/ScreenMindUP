namespace ScreenMind.Core.Settings;

/// <summary>General application behaviour.</summary>
public sealed record GeneralSettings
{
    public string Language { get; init; } = "en";

    public bool StartWithWindows { get; init; }

    public bool StartMinimizedToTray { get; init; } = true;
}

/// <summary>Overlay and chat window appearance.</summary>
public sealed record UiSettings
{
    public string Theme { get; init; } = "System";

    public double OverlayOpacity { get; init; } = 0.95;

    public int OverlayWidth { get; init; } = 420;

    public bool OverlayAlwaysOnTop { get; init; } = true;
}

/// <summary>Capture and preprocessing behaviour.</summary>
public sealed record CaptureSettings
{
    public string OutputFormat { get; init; } = "Png";

    public int JpegQuality { get; init; } = 85;

    public int MaxPayloadBytes { get; init; } = 5 * 1024 * 1024;

    public int MaxDimension { get; init; } = 3840;

    public bool ExcludeOwnWindows { get; init; } = true;
}

/// <summary>Global hotkey bindings, stored as gesture strings (e.g. "Ctrl+Shift+S").</summary>
public sealed record HotkeySettings
{
    public string CaptureActiveWindow { get; init; } = "Ctrl+Shift+W";

    public string CaptureMonitor { get; init; } = "Ctrl+Shift+M";

    public string CaptureRegion { get; init; } = "Ctrl+Shift+S";

    public string OpenChat { get; init; } = "Ctrl+Shift+C";

    public bool Enabled { get; init; } = true;
}

/// <summary>Per-provider non-secret configuration. API keys live in the secret store.</summary>
public sealed record ProviderSettings
{
    public string ActiveProviderId { get; init; } = "openai";

    public string? FallbackProviderId { get; init; }

    public IReadOnlyList<ProviderConfig> Configs { get; init; } = [];
}

/// <summary>Non-secret configuration of a single provider instance.</summary>
public sealed record ProviderConfig
{
    public required string ProviderId { get; init; }

    public string? BaseUrl { get; init; }

    public string Model { get; init; } = string.Empty;

    public int TimeoutSeconds { get; init; } = 120;
}

/// <summary>Analysis profile selection.</summary>
public sealed record ProfileSettings
{
    public string ActiveProfileId { get; init; } = "general";

    public IReadOnlyList<CustomProfile> CustomProfiles { get; init; } = [];
}

/// <summary>User-defined analysis profile.</summary>
public sealed record CustomProfile
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string SystemPrompt { get; init; }
}

/// <summary>Privacy guarantees and pre-send warnings.</summary>
public sealed record PrivacySettings
{
    public bool ShowCloudWarningBeforeFirstSend { get; init; } = true;

    public bool CloudWarningAcknowledged { get; init; }

    public IReadOnlyList<string> BlockedProcessNames { get; init; } = [];

    public IReadOnlyList<string> BlockedWindowTitlePatterns { get; init; } = [];
}

/// <summary>Update channel and cadence.</summary>
public sealed record UpdateSettings
{
    public string Channel { get; init; } = "stable";

    public bool CheckAutomatically { get; init; } = true;

    public int CheckIntervalHours { get; init; } = 24;
}
