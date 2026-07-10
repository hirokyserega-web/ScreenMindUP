using System.Text.Json.Nodes;
using ScreenMind.Core.Settings;

namespace ScreenMind.Infrastructure.Settings;

public static class SettingsMigrator
{
    private static readonly IReadOnlyDictionary<int, Action<JsonObject>> _steps =
        new Dictionary<int, Action<JsonObject>>
        {
            [1] = MigrateVersion1To2,
        };

    public static (JsonObject Node, bool Migrated) Migrate(JsonObject root)
    {
        ArgumentNullException.ThrowIfNull(root);

        var version = ReadVersion(root);
        if (version > AppSettings.CurrentSchemaVersion)
        {
            throw new NotSupportedException(
                $"Settings schema version {version} is newer than supported version {AppSettings.CurrentSchemaVersion}.");
        }

        var migrated = false;
        while (version < AppSettings.CurrentSchemaVersion)
        {
            if (!_steps.TryGetValue(version, out var step))
            {
                throw new NotSupportedException($"No migration step defined from schema version {version}.");
            }

            step(root);
            version++;
            root["schemaVersion"] = version;
            migrated = true;
        }

        return (root, migrated);
    }

    private static void MigrateVersion1To2(JsonObject root)
    {
        var general = GetOrCreateObject(root, "general");
        if (general["language"]?.GetValue<string>() == "en")
        {
            general["language"] = "ru";
        }

        var hotkeys = GetOrCreateObject(root, "hotkeys");
        hotkeys["captureMonitor"] = "Ctrl+Shift+S";
        hotkeys["captureRegion"] = "Ctrl+Shift+A";
        hotkeys["askWithScreenshot"] = hotkeys["askWithScreenshot"]?.DeepClone() ?? "Ctrl+Shift+Q";
        hotkeys["toggleInterface"] = hotkeys["toggleInterface"]?.DeepClone() ?? "Ctrl+Shift+Space";
        hotkeys["cancelCurrentAction"] = hotkeys["cancelCurrentAction"]?.DeepClone() ?? "Esc";
        hotkeys.Remove("openChat");

        var profiles = GetOrCreateObject(root, "profiles");
        if (profiles["customProfiles"] is not JsonArray customProfiles)
        {
            return;
        }

        foreach (var profile in customProfiles.OfType<JsonObject>())
        {
            profile["providerId"] = profile["providerId"]?.DeepClone() ?? "openai";
            profile["modelId"] = profile["modelId"]?.DeepClone() ?? "gpt-4o";
            profile["captureMode"] = profile["captureMode"]?.DeepClone() ?? "ActiveWindow";
            profile["streamingEnabled"] = profile["streamingEnabled"]?.DeepClone() ?? true;
            profile["timeoutSeconds"] = profile["timeoutSeconds"]?.DeepClone() ?? 120;
            profile["maxResponseCharacters"] = profile["maxResponseCharacters"]?.DeepClone() ?? 16000;
            profile["image"] = profile["image"]?.DeepClone() ?? new JsonObject
            {
                ["format"] = "Png",
                ["jpegQuality"] = 85,
                ["maxPayloadBytes"] = 5 * 1024 * 1024,
                ["maxDimension"] = 3840,
            };
        }
    }

    private static JsonObject GetOrCreateObject(JsonObject root, string propertyName)
    {
        if (root[propertyName] is JsonObject value)
        {
            return value;
        }

        value = new JsonObject();
        root[propertyName] = value;
        return value;
    }

    private static int ReadVersion(JsonObject root)
    {
        var node = root["schemaVersion"];
        if (node is null)
        {
            return 1;
        }

        try
        {
            return node.GetValue<int>();
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException)
        {
            throw new NotSupportedException("Settings schemaVersion is not a valid integer.", ex);
        }
    }
}
