using System.Text.Json.Nodes;
using ScreenMind.Core.Settings;
using ScreenMind.Infrastructure.Settings;

namespace ScreenMind.IntegrationTests.Settings;

public sealed class SettingsMigratorTests
{
    [Fact]
    public void Migrate_CurrentVersion_IsNoOp()
    {
        var root = new JsonObject { ["schemaVersion"] = AppSettings.CurrentSchemaVersion };

        var (_, migrated) = SettingsMigrator.Migrate(root);

        migrated.Should().BeFalse();
    }

    [Fact]
    public void Migrate_VersionOne_AddsCompatibleProfileAndHotkeyShape()
    {
        var root = JsonNode.Parse(
            """
            {
              "schemaVersion": 1,
              "hotkeys": { "openChat": "Ctrl+Alt+C" },
              "profiles": {
                "customProfiles": [
                  { "id": "x", "name": "X", "systemPrompt": "Help" }
                ]
              }
            }
            """)!.AsObject();

        var (migratedRoot, migrated) = SettingsMigrator.Migrate(root);
        var profile = migratedRoot["profiles"]!["customProfiles"]![0]!.AsObject();

        migrated.Should().BeTrue();
        migratedRoot["schemaVersion"]!.GetValue<int>().Should().Be(2);
        migratedRoot["hotkeys"]!["toggleInterface"]!.GetValue<string>().Should().Be("Ctrl+Shift+Space");
        migratedRoot["hotkeys"]!["askWithScreenshot"]!.GetValue<string>().Should().Be("Ctrl+Shift+Q");
        profile["providerId"]!.GetValue<string>().Should().Be("openai");
        profile["modelId"]!.GetValue<string>().Should().Be("gpt-4o");
        profile["captureMode"]!.GetValue<string>().Should().Be("ActiveWindow");
        profile["image"].Should().NotBeNull();
    }

    [Fact]
    public void Migrate_MissingVersion_TreatedAsVersionOne()
    {
        var root = new JsonObject();

        var (_, migrated) = SettingsMigrator.Migrate(root);

        migrated.Should().BeTrue();
        root["schemaVersion"]!.GetValue<int>().Should().Be(AppSettings.CurrentSchemaVersion);
    }

    [Fact]
    public void Migrate_NewerVersion_Throws()
    {
        var root = new JsonObject { ["schemaVersion"] = AppSettings.CurrentSchemaVersion + 1 };

        var act = () => SettingsMigrator.Migrate(root);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Migrate_NonIntegerVersion_Throws()
    {
        var root = new JsonObject { ["schemaVersion"] = "banana" };

        var act = () => SettingsMigrator.Migrate(root);

        act.Should().Throw<NotSupportedException>();
    }
}
