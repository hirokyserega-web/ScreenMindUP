using System.Text.RegularExpressions;

namespace ScreenMind.Core.Settings;

public sealed record SettingsValidationResult(bool IsValid, IReadOnlyList<string> Errors)
{
    public static SettingsValidationResult Valid { get; } = new(true, []);
}

public static partial class SettingsValidator
{
    private static readonly HashSet<string> _builtInProfileIds =
        new(StringComparer.OrdinalIgnoreCase) { "general", "code", "explain", "concise", "translate", "document", "ui-ux" };

    private static readonly HashSet<string> _supportedLanguages =
        new(StringComparer.OrdinalIgnoreCase) { "ru", "en" };

    private static readonly HashSet<string> _themes =
        new(StringComparer.OrdinalIgnoreCase) { "System", "Light", "Dark" };

    private static readonly HashSet<string> _captureModes =
        new(StringComparer.OrdinalIgnoreCase) { "ActiveWindow", "Monitor", "Region" };

    private static readonly HashSet<string> _imageFormats =
        new(StringComparer.OrdinalIgnoreCase) { "Png", "Jpeg", "Webp" };

    public static SettingsValidationResult Validate(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var errors = new List<string>();

        ValidateRoot(settings, errors);
        ValidateProviders(settings.Providers, errors);
        ValidateProfiles(settings.Profiles, settings.Providers, errors);
        ValidateHotkeys(settings.Hotkeys, errors);

        return errors.Count == 0 ? SettingsValidationResult.Valid : new SettingsValidationResult(false, errors);
    }

    public static void EnsureValid(AppSettings settings)
    {
        var result = Validate(settings);
        if (!result.IsValid)
        {
            throw new InvalidOperationException("Settings are invalid: " + string.Join(" ", result.Errors));
        }
    }

    private static void ValidateRoot(AppSettings settings, List<string> errors)
    {
        if (settings.SchemaVersion is < 1 or > AppSettings.CurrentSchemaVersion)
        {
            errors.Add($"SchemaVersion must be between 1 and {AppSettings.CurrentSchemaVersion}.");
        }

        if (!_supportedLanguages.Contains(settings.General.Language))
        {
            errors.Add("General.Language is not supported.");
        }

        if (!_themes.Contains(settings.Ui.Theme))
        {
            errors.Add("Ui.Theme must be System, Light or Dark.");
        }

        if (settings.Ui.OverlayOpacity is < 0.2 or > 1.0)
        {
            errors.Add("Ui.OverlayOpacity must be between 0.2 and 1.0.");
        }

        if (settings.Ui.OverlayWidth is < 240 or > 1200)
        {
            errors.Add("Ui.OverlayWidth must be between 240 and 1200.");
        }

        if (!_captureModes.Contains(settings.Capture.Mode))
        {
            errors.Add("Capture.Mode must be ActiveWindow, Monitor or Region.");
        }

        ValidateImage(
            settings.Capture.OutputFormat,
            settings.Capture.JpegQuality,
            settings.Capture.MaxPayloadBytes,
            settings.Capture.MaxDimension,
            "Capture",
            errors);

        if (settings.Updates.Channel is not ("stable" or "prerelease"))
        {
            errors.Add("Updates.Channel must be 'stable' or 'prerelease'.");
        }

        if (settings.Updates.CheckIntervalHours is < 1 or > 720)
        {
            errors.Add("Updates.CheckIntervalHours must be between 1 and 720.");
        }
    }

    private static void ValidateProviders(ProviderSettings providers, List<string> errors)
    {
        var configs = providers.Configs;
        foreach (var config in configs)
        {
            if (string.IsNullOrWhiteSpace(config.ProviderId))
            {
                errors.Add("Providers.Configs contains an empty provider id.");
            }
        }

        var duplicateIds = configs
            .Where(c => !string.IsNullOrWhiteSpace(c.ProviderId))
            .GroupBy(c => c.ProviderId, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicateIds.Count > 0)
        {
            errors.Add($"Providers.Configs contains duplicate provider ids: {string.Join(", ", duplicateIds)}.");
        }

        var providerIds = configs.Select(c => c.ProviderId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(providers.ActiveProviderId) || !providerIds.Contains(providers.ActiveProviderId))
        {
            errors.Add("Providers.ActiveProviderId must reference an existing provider config.");
        }

        if (providers.FallbackProviderId is not null)
        {
            if (!providerIds.Contains(providers.FallbackProviderId))
            {
                errors.Add("Providers.FallbackProviderId must reference an existing provider config.");
            }

            if (string.Equals(providers.ActiveProviderId, providers.FallbackProviderId, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Providers.FallbackProviderId must differ from ActiveProviderId.");
            }
        }

        foreach (var config in configs)
        {
            if (string.IsNullOrWhiteSpace(config.Model))
            {
                errors.Add($"Provider '{config.ProviderId}': Model must not be empty.");
            }

            if (config.TimeoutSeconds is < 5 or > 600)
            {
                errors.Add($"Provider '{config.ProviderId}': TimeoutSeconds must be between 5 and 600.");
            }

            ValidateBaseUrl(config, errors);
        }
    }

    private static void ValidateProfiles(ProfileSettings profiles, ProviderSettings providers, List<string> errors)
    {
        var customIds = profiles.CustomProfiles.Select(p => p.Id).ToList();
        var duplicates = customIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .GroupBy(id => id, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();
        if (duplicates.Count > 0)
        {
            errors.Add($"Profiles.CustomProfiles contains duplicate profile ids: {string.Join(", ", duplicates)}.");
        }

        var allIds = new HashSet<string>(_builtInProfileIds, StringComparer.OrdinalIgnoreCase);
        allIds.UnionWith(customIds.Where(id => !string.IsNullOrWhiteSpace(id)));
        if (string.IsNullOrWhiteSpace(profiles.ActiveProfileId) || !allIds.Contains(profiles.ActiveProfileId))
        {
            errors.Add("Profiles.ActiveProfileId must reference a built-in or custom profile.");
        }

        var providerIds = providers.Configs.Select(c => c.ProviderId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var profile in profiles.CustomProfiles)
        {
            if (string.IsNullOrWhiteSpace(profile.Id) || string.IsNullOrWhiteSpace(profile.Name))
            {
                errors.Add("Custom profiles must have a non-empty Id and Name.");
            }

            if (_builtInProfileIds.Contains(profile.Id))
            {
                errors.Add($"Custom profile '{profile.Id}' conflicts with a built-in profile id.");
            }

            if (string.IsNullOrWhiteSpace(profile.SystemPrompt))
            {
                errors.Add($"Custom profile '{profile.Id}': SystemPrompt must not be empty.");
            }

            if (profile.ProviderId is not null && !providerIds.Contains(profile.ProviderId))
            {
                errors.Add($"Custom profile '{profile.Id}': ProviderId must reference an existing provider.");
            }

            if (profile.FallbackProviderId is not null && !providerIds.Contains(profile.FallbackProviderId))
            {
                errors.Add($"Custom profile '{profile.Id}': FallbackProviderId must reference an existing provider.");
            }

            if (profile.ProviderId is not null &&
                string.Equals(profile.ProviderId, profile.FallbackProviderId, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Custom profile '{profile.Id}': fallback must differ from provider.");
            }

            if (profile.ProviderId is not null && string.IsNullOrWhiteSpace(profile.ModelId))
            {
                errors.Add($"Custom profile '{profile.Id}': ModelId is required when ProviderId is set.");
            }

            if (!_captureModes.Contains(profile.CaptureMode))
            {
                errors.Add($"Custom profile '{profile.Id}': CaptureMode is invalid.");
            }

            if (profile.TimeoutSeconds is < 5 or > 600)
            {
                errors.Add($"Custom profile '{profile.Id}': TimeoutSeconds must be between 5 and 600.");
            }

            if (profile.MaxResponseCharacters is < 256 or > 200000)
            {
                errors.Add($"Custom profile '{profile.Id}': MaxResponseCharacters must be between 256 and 200000.");
            }

            ValidateImage(
                profile.Image.Format,
                profile.Image.JpegQuality,
                profile.Image.MaxPayloadBytes,
                profile.Image.MaxDimension,
                $"Custom profile '{profile.Id}'.Image",
                errors);
        }
    }

    private static void ValidateHotkeys(HotkeySettings hotkeys, List<string> errors)
    {
        var bindings = new Dictionary<string, string>
        {
            [nameof(hotkeys.CaptureActiveWindow)] = hotkeys.CaptureActiveWindow,
            [nameof(hotkeys.CaptureMonitor)] = hotkeys.CaptureMonitor,
            [nameof(hotkeys.CaptureRegion)] = hotkeys.CaptureRegion,
            [nameof(hotkeys.AskWithScreenshot)] = hotkeys.AskWithScreenshot,
            [nameof(hotkeys.ToggleInterface)] = hotkeys.ToggleInterface,
            [nameof(hotkeys.CancelCurrentAction)] = hotkeys.CancelCurrentAction,
        };

        foreach (var binding in bindings)
        {
            if (!IsValidHotkey(binding.Value))
            {
                errors.Add($"Hotkeys.{binding.Key} has an invalid format.");
            }
        }

        var conflicts = bindings
            .GroupBy(pair => NormalizeHotkey(pair.Value), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .ToList();
        foreach (var conflict in conflicts)
        {
            errors.Add($"Hotkey '{conflict.Key}' is assigned more than once.");
        }
    }

    private static void ValidateBaseUrl(ProviderConfig config, List<string> errors)
    {
        if (config.BaseUrl is null)
        {
            return;
        }

        if (!Uri.TryCreate(config.BaseUrl, UriKind.Absolute, out var uri) || uri.UserInfo.Length > 0)
        {
            errors.Add($"Provider '{config.ProviderId}': BaseUrl is not a valid absolute URL without credentials.");
            return;
        }

        if (uri.Scheme == Uri.UriSchemeHttps)
        {
            return;
        }

        if (uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)
        {
            return;
        }

        errors.Add($"Provider '{config.ProviderId}': remote BaseUrl must use HTTPS; HTTP is allowed only for loopback.");
    }

    private static void ValidateImage(
        string format,
        int jpegQuality,
        int maxPayloadBytes,
        int maxDimension,
        string prefix,
        List<string> errors)
    {
        if (!_imageFormats.Contains(format))
        {
            errors.Add($"{prefix}.OutputFormat must be Png, Jpeg or Webp.");
        }

        if (jpegQuality is < 1 or > 100)
        {
            errors.Add($"{prefix}.JpegQuality must be between 1 and 100.");
        }

        if (maxPayloadBytes is < 64 * 1024 or > 50 * 1024 * 1024)
        {
            errors.Add($"{prefix}.MaxPayloadBytes must be between 64 KiB and 50 MiB.");
        }

        if (maxDimension is < 256 or > 16384)
        {
            errors.Add($"{prefix}.MaxDimension must be between 256 and 16384.");
        }
    }

    private static bool IsValidHotkey(string value)
    {
        if (string.Equals(value, "Esc", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(value) || !HotkeyPattern().IsMatch(value))
        {
            return false;
        }

        var parts = value.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var modifiers = parts[..^1];
        return modifiers.Distinct(StringComparer.OrdinalIgnoreCase).Count() == modifiers.Length;
    }

    private static string NormalizeHotkey(string value) => value.Trim().ToUpperInvariant();

    [GeneratedRegex("^(?:(?:Ctrl|Alt|Shift|Win)\\+)+(?:[A-Z0-9]|F(?:[1-9]|1[0-2])|Space|Enter|Tab|Esc)$", RegexOptions.IgnoreCase)]
    private static partial Regex HotkeyPattern();
}
