using ScreenMind.Core.Settings;

namespace ScreenMind.Core.Tests.Settings;

public sealed class SettingsValidatorTests
{
    [Fact]
    public void Validate_DefaultSettings_IsValid()
    {
        var settings = new AppSettings();

        var result = SettingsValidator.Validate(settings);

        result.IsValid.Should().BeTrue();
        settings.General.Language.Should().Be("ru");
        settings.Hotkeys.CaptureActiveWindow.Should().Be("Ctrl+Shift+W");
        settings.Hotkeys.CaptureMonitor.Should().Be("Ctrl+Shift+S");
        settings.Hotkeys.CaptureRegion.Should().Be("Ctrl+Shift+A");
        settings.Hotkeys.AskWithScreenshot.Should().Be("Ctrl+Shift+Q");
        settings.Hotkeys.ToggleInterface.Should().Be("Ctrl+Shift+Space");
        settings.Hotkeys.CancelCurrentAction.Should().Be("Esc");
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(1.5)]
    public void Validate_OverlayOpacityOutOfRange_Fails(double opacity)
    {
        var settings = new AppSettings { Ui = new UiSettings { OverlayOpacity = opacity } };

        var result = SettingsValidator.Validate(settings);

        result.Errors.Should().ContainSingle(e => e.Contains("OverlayOpacity"));
    }

    [Fact]
    public void Validate_DuplicateOrEmptyProviderIds_Fails()
    {
        var settings = CreateSettings(
            configs:
            [
                new ProviderConfig { ProviderId = "openai", Model = "gpt-4o" },
                new ProviderConfig { ProviderId = "OPENAI", Model = "other" },
                new ProviderConfig { ProviderId = " ", Model = "other" },
            ]);

        var result = SettingsValidator.Validate(settings);

        result.Errors.Should().Contain(e => e.Contains("duplicate", StringComparison.OrdinalIgnoreCase));
        result.Errors.Should().Contain(e => e.Contains("empty provider id", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ProviderReferencesMustExistAndFallbackMustDiffer_Fails()
    {
        var missing = CreateSettings(activeProviderId: "missing", fallbackProviderId: "fallback");
        var same = CreateSettings(fallbackProviderId: "openai");

        var missingResult = SettingsValidator.Validate(missing);
        var sameResult = SettingsValidator.Validate(same);

        missingResult.Errors.Should().Contain(e => e.Contains("ActiveProviderId"));
        missingResult.Errors.Should().Contain(e => e.Contains("FallbackProviderId"));
        sameResult.Errors.Should().Contain(e => e.Contains("must differ"));
    }

    [Fact]
    public void Validate_RemoteHttpBaseUrl_Fails_ButLoopbackHttpPasses()
    {
        var remote = CreateSettings(
            configs: [new ProviderConfig { ProviderId = "openai", Model = "model", BaseUrl = "http://example.com/v1" }]);
        var loopback = CreateSettings(
            configs: [new ProviderConfig { ProviderId = "openai", Model = "model", BaseUrl = "http://127.0.0.1:11434" }]);

        var remoteResult = SettingsValidator.Validate(remote);
        var loopbackResult = SettingsValidator.Validate(loopback);

        remoteResult.Errors.Should().ContainSingle(e => e.Contains("HTTPS"));
        loopbackResult.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_MissingModel_Fails()
    {
        var settings = CreateSettings(
            configs: [new ProviderConfig { ProviderId = "openai", Model = " " }]);

        SettingsValidator.Validate(settings).Errors.Should().ContainSingle(e => e.Contains("Model"));
    }

    [Theory]
    [InlineData("xx")]
    public void Validate_UnsupportedLanguage_Fails(string language)
    {
        var settings = new AppSettings { General = new GeneralSettings { Language = language } };

        SettingsValidator.Validate(settings).Errors.Should().ContainSingle(e => e.Contains("Language"));
    }

    [Theory]
    [InlineData("Neon")]
    [InlineData("")]
    public void Validate_UnsupportedTheme_Fails(string theme)
    {
        var settings = new AppSettings { Ui = new UiSettings { Theme = theme } };

        SettingsValidator.Validate(settings).Errors.Should().ContainSingle(e => e.Contains("Theme"));
    }

    [Fact]
    public void Validate_HotkeySyntaxAndConflicts_Fails()
    {
        var settings = new AppSettings
        {
            Hotkeys = new HotkeySettings
            {
                CaptureActiveWindow = "Ctrl+Shift+W",
                CaptureMonitor = "Ctrl+Shift+W",
                CaptureRegion = "bad++gesture",
            },
        };

        var result = SettingsValidator.Validate(settings);

        result.Errors.Should().Contain(e => e.Contains("invalid format"));
        result.Errors.Should().Contain(e => e.Contains("assigned more than once"));
    }

    [Fact]
    public void Validate_ProfileReferencesAndUniqueness_Fails()
    {
        var settings = new AppSettings
        {
            Profiles = new ProfileSettings
            {
                ActiveProfileId = "missing",
                CustomProfiles =
                [
                    CreateProfile("custom"),
                    CreateProfile("CUSTOM"),
                ],
            },
        };

        var result = SettingsValidator.Validate(settings);

        result.Errors.Should().Contain(e => e.Contains("ActiveProfileId"));
        result.Errors.Should().Contain(e => e.Contains("duplicate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_ExtendedProfileFields_FailsWhenUnsafe()
    {
        var profile = CreateProfile("custom") with
        {
            ProviderId = "missing",
            FallbackProviderId = "missing",
            CaptureMode = "Desktop",
            TimeoutSeconds = 1,
            MaxResponseCharacters = 10,
            Image = new ProfileImageSettings { Format = "Bmp", MaxDimension = 1, JpegQuality = 0 },
        };
        var settings = new AppSettings
        {
            Profiles = new ProfileSettings { ActiveProfileId = "custom", CustomProfiles = [profile] },
        };

        var result = SettingsValidator.Validate(settings);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.Contains("ProviderId"));
        result.Errors.Should().Contain(e => e.Contains("CaptureMode"));
        result.Errors.Should().Contain(e => e.Contains("TimeoutSeconds"));
        result.Errors.Should().Contain(e => e.Contains("MaxResponseCharacters"));
        result.Errors.Should().Contain(e => e.Contains("Image.OutputFormat"));
    }

    [Fact]
    public void Validate_CaptureAndProviderBounds_CollectsErrors()
    {
        var settings = CreateSettings(
            configs: [new ProviderConfig { ProviderId = "openai", Model = "model", TimeoutSeconds = 1 }]) with
        {
            Capture = new CaptureSettings
            {
                JpegQuality = 0,
                MaxPayloadBytes = 1024,
                MaxDimension = 1,
                OutputFormat = "Bmp",
            },
        };

        var result = SettingsValidator.Validate(settings);

        result.Errors.Should().HaveCountGreaterThan(4);
    }

    private static AppSettings CreateSettings(
        string activeProviderId = "openai",
        string? fallbackProviderId = null,
        IReadOnlyList<ProviderConfig>? configs = null) =>
        new()
        {
            Providers = new ProviderSettings
            {
                ActiveProviderId = activeProviderId,
                FallbackProviderId = fallbackProviderId,
                Configs = configs ?? [new ProviderConfig { ProviderId = "openai", Model = "gpt-4o" }],
            },
        };

    private static CustomProfile CreateProfile(string id) =>
        new()
        {
            Id = id,
            Name = "Custom",
            ProviderId = "openai",
            ModelId = "gpt-4o",
            CaptureMode = "ActiveWindow",
            SystemPrompt = "Help",
        };
}
