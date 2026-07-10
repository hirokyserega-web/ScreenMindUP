namespace ScreenMind.Core.Settings;

/// <summary>
/// Root of the typed application settings tree. Persisted as JSON by
/// <see cref="Abstractions.ISettingsStore"/> implementations. Secrets are
/// never part of this tree — they live in the platform secret store.
/// </summary>
public sealed record AppSettings
{
    /// <summary>Current settings schema version. Bump when the shape changes and add a migration.</summary>
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public GeneralSettings General { get; init; } = new();

    public UiSettings Ui { get; init; } = new();

    public CaptureSettings Capture { get; init; } = new();

    public HotkeySettings Hotkeys { get; init; } = new();

    public ProviderSettings Providers { get; init; } = new();

    public ProfileSettings Profiles { get; init; } = new();

    public PrivacySettings Privacy { get; init; } = new();

    public UpdateSettings Updates { get; init; } = new();
}
