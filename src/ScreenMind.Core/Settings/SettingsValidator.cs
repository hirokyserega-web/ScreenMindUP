namespace ScreenMind.Core.Settings;

/// <summary>Result of validating a settings tree.</summary>
public sealed record SettingsValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static SettingsValidationResult Valid { get; } = new(true, []);
}

/// <summary>
/// Validates <see cref="AppSettings"/> before persisting or after loading.
/// Invalid trees must never be written to disk.
/// </summary>
public static class SettingsValidator
{
    public static SettingsValidationResult Validate(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var errors = new List<string>();

        if (settings.SchemaVersion is < 1 or > AppSettings.CurrentSchemaVersion)
        {
            errors.Add($"SchemaVersion must be between 1 and {AppSettings.CurrentSchemaVersion}.");
        }

        if (settings.Ui.OverlayOpacity is < 0.2 or > 1.0)
        {
            errors.Add("Ui.OverlayOpacity must be between 0.2 and 1.0.");
        }

        if (settings.Ui.OverlayWidth is < 240 or > 1200)
        {
            errors.Add("Ui.OverlayWidth must be between 240 and 1200.");
        }

        if (settings.Capture.JpegQuality is < 1 or > 100)
        {
            errors.Add("Capture.JpegQuality must be between 1 and 100.");
        }

        if (settings.Capture.MaxPayloadBytes < 64 * 1024)
        {
            errors.Add("Capture.MaxPayloadBytes must be at least 64 KiB.");
        }

        if (settings.Capture.MaxDimension is < 256 or > 16384)
        {
            errors.Add("Capture.MaxDimension must be between 256 and 16384.");
        }

        if (settings.Capture.OutputFormat is not ("Png" or "Jpeg" or "Webp"))
        {
            errors.Add("Capture.OutputFormat must be Png, Jpeg or Webp.");
        }

        if (string.IsNullOrWhiteSpace(settings.Providers.ActiveProviderId))
        {
            errors.Add("Providers.ActiveProviderId must not be empty.");
        }

        var duplicateProviderIds = settings.Providers.Configs
            .GroupBy(c => c.ProviderId, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicateProviderIds.Count > 0)
        {
            errors.Add($"Providers.Configs contains duplicate provider ids: {string.Join(", ", duplicateProviderIds)}.");
        }

        foreach (var config in settings.Providers.Configs)
        {
            if (config.TimeoutSeconds is < 5 or > 600)
            {
                errors.Add($"Provider '{config.ProviderId}': TimeoutSeconds must be between 5 and 600.");
            }

            if (config.BaseUrl is not null && !Uri.TryCreate(config.BaseUrl, UriKind.Absolute, out var uri))
            {
                errors.Add($"Provider '{config.ProviderId}': BaseUrl is not a valid absolute URL.");
            }
            else if (config.BaseUrl is not null)
            {
                _ = Uri.TryCreate(config.BaseUrl, UriKind.Absolute, out var parsed);
                if (parsed!.Scheme is not ("http" or "https"))
                {
                    errors.Add($"Provider '{config.ProviderId}': BaseUrl must use http or https.");
                }
            }
        }

        if (string.IsNullOrWhiteSpace(settings.Profiles.ActiveProfileId))
        {
            errors.Add("Profiles.ActiveProfileId must not be empty.");
        }

        foreach (var profile in settings.Profiles.CustomProfiles)
        {
            if (string.IsNullOrWhiteSpace(profile.Id) || string.IsNullOrWhiteSpace(profile.Name))
            {
                errors.Add("Custom profiles must have a non-empty Id and Name.");
            }

            if (string.IsNullOrWhiteSpace(profile.SystemPrompt))
            {
                errors.Add($"Custom profile '{profile.Id}': SystemPrompt must not be empty.");
            }
        }

        if (settings.Updates.Channel is not ("stable" or "prerelease"))
        {
            errors.Add("Updates.Channel must be 'stable' or 'prerelease'.");
        }

        if (settings.Updates.CheckIntervalHours is < 1 or > 720)
        {
            errors.Add("Updates.CheckIntervalHours must be between 1 and 720.");
        }

        return errors.Count == 0 ? SettingsValidationResult.Valid : new SettingsValidationResult(false, errors);
    }

    /// <summary>Throws when <paramref name="settings"/> is invalid.</summary>
    public static void EnsureValid(AppSettings settings)
    {
        var result = Validate(settings);
        if (!result.IsValid)
        {
            throw new InvalidOperationException(
                "Settings are invalid: " + string.Join(" ", result.Errors));
        }
    }
}
