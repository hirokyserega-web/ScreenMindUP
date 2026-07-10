using ScreenMind.Core.Settings;

namespace ScreenMind.Core.Tests.Settings;

public sealed class SettingsValidatorTests
{
    [Fact]
    public void Validate_DefaultSettings_IsValid()
    {
        var result = SettingsValidator.Validate(new AppSettings());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(1.5)]
    public void Validate_OverlayOpacityOutOfRange_Fails(double opacity)
    {
        var settings = new AppSettings { Ui = new UiSettings { OverlayOpacity = opacity } };

        var result = SettingsValidator.Validate(settings);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Contains("OverlayOpacity"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Validate_JpegQualityOutOfRange_Fails(int quality)
    {
        var settings = new AppSettings { Capture = new CaptureSettings { JpegQuality = quality } };

        var result = SettingsValidator.Validate(settings);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Contains("JpegQuality"));
    }

    [Fact]
    public void Validate_UnknownOutputFormat_Fails()
    {
        var settings = new AppSettings { Capture = new CaptureSettings { OutputFormat = "Bmp" } };

        var result = SettingsValidator.Validate(settings);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Contains("OutputFormat"));
    }

    [Fact]
    public void Validate_TinyMaxPayload_Fails()
    {
        var settings = new AppSettings { Capture = new CaptureSettings { MaxPayloadBytes = 1024 } };

        var result = SettingsValidator.Validate(settings);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Contains("MaxPayloadBytes"));
    }

    [Fact]
    public void Validate_EmptyActiveProviderId_Fails()
    {
        var settings = new AppSettings
        {
            Providers = new ProviderSettings { ActiveProviderId = " " },
        };

        var result = SettingsValidator.Validate(settings);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Contains("ActiveProviderId"));
    }

    [Fact]
    public void Validate_DuplicateProviderConfigs_Fails()
    {
        var settings = new AppSettings
        {
            Providers = new ProviderSettings
            {
                Configs =
                [
                    new ProviderConfig { ProviderId = "openai", Model = "gpt-4o" },
                    new ProviderConfig { ProviderId = "openai", Model = "gpt-4o-mini" },
                ],
            },
        };

        var result = SettingsValidator.Validate(settings);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Contains("duplicate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_CustomProfileWithoutPrompt_Fails()
    {
        var settings = new AppSettings
        {
            Profiles = new ProfileSettings
            {
                CustomProfiles = [new CustomProfile { Id = "x", Name = "X", SystemPrompt = "" }],
            },
        };

        var result = SettingsValidator.Validate(settings);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_CollectsMultipleErrors()
    {
        var settings = new AppSettings
        {
            Ui = new UiSettings { OverlayOpacity = 5, OverlayWidth = 10 },
            Capture = new CaptureSettings { JpegQuality = 0 },
        };

        var result = SettingsValidator.Validate(settings);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThan(2);
    }
}
