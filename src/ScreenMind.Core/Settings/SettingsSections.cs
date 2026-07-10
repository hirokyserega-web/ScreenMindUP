namespace ScreenMind.Core.Settings;

public sealed record GeneralSettings
{
    public string Language { get; init; } = "ru";

    public bool StartWithWindows { get; init; }

    public bool StartMinimizedToTray { get; init; } = true;
}

public sealed record UiSettings
{
    public string Theme { get; init; } = "System";

    public double OverlayOpacity { get; init; } = 0.95;

    public int OverlayWidth { get; init; } = 420;

    public bool OverlayAlwaysOnTop { get; init; } = true;
}

public sealed record CaptureSettings
{
    public string Mode { get; init; } = "ActiveWindow";

    public string OutputFormat { get; init; } = "Png";

    public int JpegQuality { get; init; } = 85;

    public int MaxPayloadBytes { get; init; } = 5 * 1024 * 1024;

    public int MaxDimension { get; init; } = 3840;

    public bool ExcludeOwnWindows { get; init; } = true;
}

public sealed record HotkeySettings
{
    public string CaptureActiveWindow { get; init; } = "Ctrl+Shift+W";

    public string CaptureMonitor { get; init; } = "Ctrl+Shift+S";

    public string CaptureRegion { get; init; } = "Ctrl+Shift+A";

    public string AskWithScreenshot { get; init; } = "Ctrl+Shift+Q";

    public string ToggleInterface { get; init; } = "Ctrl+Shift+Space";

    public string CancelCurrentAction { get; init; } = "Esc";

    public bool Enabled { get; init; } = true;
}

public sealed record ProviderSettings
{
    public string ActiveProviderId { get; init; } = "openai";

    public string? FallbackProviderId { get; init; }

    public IReadOnlyList<ProviderConfig> Configs { get; init; } =
    [
        new ProviderConfig { ProviderId = "openai", Model = "gpt-4o" },
    ];
}

public sealed record ProviderConfig
{
    public required string ProviderId { get; init; }

    public string? BaseUrl { get; init; }

    public string Model { get; init; } = string.Empty;

    public int TimeoutSeconds { get; init; } = 120;
}

public sealed record ProfileSettings
{
    public string ActiveProfileId { get; init; } = "general";

    public IReadOnlyList<CustomProfile> CustomProfiles { get; init; } = [];
}

public sealed record CustomProfile
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string SystemPrompt { get; init; }

    public string? ProviderId { get; init; }

    public string? ModelId { get; init; }

    public string? FallbackProviderId { get; init; }

    public string CaptureMode { get; init; } = "ActiveWindow";

    public bool StreamingEnabled { get; init; } = true;

    public int TimeoutSeconds { get; init; } = 120;

    public int MaxResponseCharacters { get; init; } = 16000;

    public ProfileImageSettings Image { get; init; } = new();
}

public sealed record ProfileImageSettings
{
    public string Format { get; init; } = "Png";

    public int JpegQuality { get; init; } = 85;

    public int MaxPayloadBytes { get; init; } = 5 * 1024 * 1024;

    public int MaxDimension { get; init; } = 3840;
}

public sealed record PrivacySettings
{
    public bool ShowCloudWarningBeforeFirstSend { get; init; } = true;

    public bool CloudWarningAcknowledged { get; init; }

    public IReadOnlyList<string> BlockedProcessNames { get; init; } = [];

    public IReadOnlyList<string> BlockedWindowTitlePatterns { get; init; } = [];
}

public sealed record UpdateSettings
{
    public string Channel { get; init; } = "stable";

    public bool CheckAutomatically { get; init; } = true;

    public int CheckIntervalHours { get; init; } = 24;
}
