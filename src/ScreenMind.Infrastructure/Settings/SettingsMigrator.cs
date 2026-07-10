using System.Text.Json.Nodes;
using ScreenMind.Core.Settings;

namespace ScreenMind.Infrastructure.Settings;

/// <summary>
/// Migrates persisted settings JSON between schema versions. Each registered
/// step upgrades exactly one version; steps are applied sequentially until the
/// document reaches <see cref="AppSettings.CurrentSchemaVersion"/>.
/// </summary>
public static class SettingsMigrator
{
    /// <summary>
    /// Migration steps keyed by source version. A step receives the JSON tree
    /// at version N and mutates it to be valid at version N + 1.
    /// Version 1 is the initial schema, so the table is empty until version 2 exists.
    /// </summary>
    private static readonly IReadOnlyDictionary<int, Action<JsonObject>> _steps =
        new Dictionary<int, Action<JsonObject>>();

    /// <summary>
    /// Upgrades the raw JSON tree to the current schema version.
    /// Returns the modified node and whether any migration step ran.
    /// </summary>
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
