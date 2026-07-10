using System.Text.Json;
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
        var settings = await CreateStore().LoadAsync(CancellationToken.None);

        settings.SchemaVersion.Should().Be(AppSettings.CurrentSchemaVersion);
        File.Exists(SettingsPath).Should().BeTrue();
    }

    [Fact]
    public async Task SaveThenLoad_RoundTripsValues()
    {
        var store = CreateStore();
        var settings = new AppSettings
        {
            General = new GeneralSettings { Language = "en", StartWithWindows = true },
            Ui = new UiSettings { Theme = "Dark", OverlayOpacity = 0.8 },
        };

        await store.SaveAsync(settings, CancellationToken.None);
        var loaded = await CreateStore().LoadAsync(CancellationToken.None);

        loaded.General.Language.Should().Be("en");
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
            new AppSettings { General = new GeneralSettings { Language = "en" } },
            CancellationToken.None);

        File.Exists(SettingsPath + ".bak").Should().BeTrue();
    }

    [Fact]
    public async Task Recovery_PreservesValidBackup_AndCanRecoverTwice()
    {
        var store = CreateStore();
        await store.SaveAsync(new AppSettings(), CancellationToken.None);
        await store.SaveAsync(new AppSettings(), CancellationToken.None);

        await File.WriteAllTextAsync(SettingsPath, "{ corrupt primary");
        var first = await CreateStore().LoadAsync(CancellationToken.None);

        first.General.Language.Should().Be("ru");
        await AssertValidSettingsFileAsync(SettingsPath + ".bak");
        Directory.EnumerateFiles(_dir, "*.corrupt-*").Should().ContainSingle();

        await File.WriteAllTextAsync(SettingsPath, "{ corrupt again");
        var second = await CreateStore().LoadAsync(CancellationToken.None);

        second.General.Language.Should().Be("ru");
        await AssertValidSettingsFileAsync(SettingsPath + ".bak");
        Directory.EnumerateFiles(_dir, "*.corrupt-*").Should().HaveCount(2);
    }

    [Fact]
    public async Task Recovery_ReplacesStaleTempFile()
    {
        var store = CreateStore();
        await store.SaveAsync(new AppSettings(), CancellationToken.None);
        await store.SaveAsync(new AppSettings(), CancellationToken.None);
        await File.WriteAllTextAsync(SettingsPath, "broken");
        await File.WriteAllTextAsync(SettingsPath + ".tmp", "stale temp");

        await CreateStore().LoadAsync(CancellationToken.None);

        File.Exists(SettingsPath + ".tmp").Should().BeFalse();
        await AssertValidSettingsFileAsync(SettingsPath);
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
        var invalid = new AppSettings { Ui = new UiSettings { OverlayOpacity = 5.0 } };

        var act = () => store.SaveAsync(invalid, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        File.Exists(SettingsPath).Should().BeFalse();
    }

    [Fact]
    public async Task Load_WhenSchemaVersionNewerThanSupported_FallsBackSafely()
    {
        Directory.CreateDirectory(_dir);
        await File.WriteAllTextAsync(SettingsPath, """{ "schemaVersion": 999 }""");

        var loaded = await CreateStore().LoadAsync(CancellationToken.None);

        loaded.SchemaVersion.Should().Be(AppSettings.CurrentSchemaVersion);
    }

    [Fact]
    public async Task Load_WhenMalformedJsonContainsSensitiveText_DoesNotLogIt()
    {
        Directory.CreateDirectory(_dir);
        const string sensitive = "prompt-and-secret-must-not-leak";
        await File.WriteAllTextAsync(SettingsPath, $"{{ \"systemPrompt\": \"{sensitive}\", broken }}");
        var logger = new CapturingLogger<JsonSettingsStore>();

        await new JsonSettingsStore(SettingsPath, logger).LoadAsync(CancellationToken.None);

        logger.Messages.Should().NotContain(message => message.Contains(sensitive, StringComparison.Ordinal));
    }

    private static async Task AssertValidSettingsFileAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path);
        var settings = JsonSerializer.Deserialize<AppSettings>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        settings.Should().NotBeNull();
        SettingsValidator.Validate(settings!).IsValid.Should().BeTrue();
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

    private sealed class CapturingLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(
            Microsoft.Extensions.Logging.LogLevel logLevel,
            Microsoft.Extensions.Logging.EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter) =>
            Messages.Add(formatter(state, exception));
    }
}
