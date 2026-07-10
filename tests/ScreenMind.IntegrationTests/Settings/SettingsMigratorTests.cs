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
    public void Migrate_MissingVersion_TreatedAsVersion1()
    {
        var root = new JsonObject();

        var (_, migrated) = SettingsMigrator.Migrate(root);

        migrated.Should().BeFalse();
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
