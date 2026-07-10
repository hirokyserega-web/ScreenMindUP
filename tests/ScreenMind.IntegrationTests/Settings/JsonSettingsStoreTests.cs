using Microsoft.Extensions.Logging.Abstractions;
using ScreenMind.Core.Settings;
using ScreenMind.Infrastructure.Settings;

namespace ScreenMind.IntegrationTests.Settings;

public sealed class JsonSettingsStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(
        Path.GetTempPath(), "screenmind-tests", Guid.NewGuid().ToString("N"));

    private string SettingsPath => Path.Combine(_dir, "settings.json");

    private JsonSettingsStore CreateStore() =>
        new(SettingsPath, NullLogger<JsonSettingsStore>.Instance);

    [Fact]
    public async Task Load_WhenFileMissing_CreatesDefaults()
    {
        var store = CreateStore();

        var settings = await store.LoadAsync(CancellationToken.None);

        settings.SchemaVersion.Should().Be(AppSettings.CurrentSchemaVersion);
        File.Exists(SettingsPath).Should().BeTrue();
    }

    [Fact]
    public async Task SaveThenLoad_RoundTripsValues()
    {
        var store = CreateStore();
        var settings = new AppSettings
        {
            General = new GeneralSettings { Language = "ru", StartWithWindows = true },
            Ui = new UiSettings { Theme = "Dark", OverlayOpacity = 0.8 },
        };

        await store.SaveAsync(settings, CancellationToken.None);
        var loaded = await CreateStore().LoadAsync(CancellationToken.None);

        loaded.General.Language.Should().Be("ru");
        loaded.General.StartWithWindows.Should().BeTrue();
        loaded.Ui.Theme.Should().Be("Dark");
        loaded.Ui.OverlayOpacity.Should().Be(0.8);
    }

    [Fact]
    public async Task Save_CreatesBackupOfPreviousFile()
    {
        var store = CreateStore();
        await store.SaveAsync(new AppSettings(), CancellationToken.None);
        await store.SaveAsync(
            new AppSettings { General = new GeneralSettings { Language = "de" } },
            CancellationToken.None);

        File.Exists(SettingsPath + ".bak").Should().BeTrue();
    }

    [Fact]
    public async Task Load_WhenFileCorrupted_RecoversFromBackup()
    {
        var store = CreateStore();
        await store.SaveAsync(
            new AppSettings { General = new GeneralSettings { Language = "fr" } },
            CancellationToken.None);
        await store.SaveAsync(
            new AppSettings { General = new GeneralSettings { Language = "fr" } },
            CancellationToken.None);

        await File.WriteAllTextAsync(SettingsPath, "{ this is not json !!!");

        var loaded = await CreateStore().LoadAsync(CancellationToken.None);

        loaded.General.Language.Should().Be("fr");
    }

    [Fact]
    public async Task Load_WhenFileAndBackupCorrupted_FallsBackToDefaults()
    {
        Directory.CreateDirectory(_dir);
        await File.WriteAllTextAsync(SettingsPath, "not json");
        await File.WriteAllTextAsync(SettingsPath + ".bak", "also not json");

        var loaded = await CreateStore().LoadAsync(CancellationToken.None);

        loaded.SchemaVersion.Should().Be(AppSettings.CurrentSchemaVersion);
        Directory.EnumerateFiles(_dir, "*.corrupt-*").Should().NotBeEmpty();
    }

    [Fact]
    public async Task Save_WhenSettingsInvalid_ThrowsAndDoesNotWrite()
    {
        var store = CreateStore();
        var invalid = new AppSettings
        {
            Ui = new UiSettings { OverlayOpacity = 5.0 },
        };

        var act = () => store.SaveAsync(invalid, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        File.Exists(SettingsPath).Should().BeFalse();
    }

    [Fact]
    public async Task Load_WhenSchemaVersionNewerThanSupported_FallsBackSafely()
    {
        Directory.CreateDirectory(_dir);
        await File.WriteAllTextAsync(
            SettingsPath, """{ "schemaVersion": 999 }""");

        var loaded = await CreateStore().LoadAsync(CancellationToken.None);

        loaded.SchemaVersion.Should().Be(AppSettings.CurrentSchemaVersion);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_dir, recursive: true);
        }
        catch (IOException)
        {
        }
    }
}
