using Microsoft.Extensions.DependencyInjection;
using ScreenMind.Core.Abstractions;
using ScreenMind.Core.Settings;
using ScreenMind.Infrastructure.DependencyInjection;
using ScreenMind.Platform.Windows.DependencyInjection;
using ScreenMind.Platform.Windows.Security;

namespace ScreenMind.IntegrationTests.Composition;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void PhaseThreeServices_RegisterExpectedLifetimes()
    {
        var services = new ServiceCollection();
        services.AddScreenMindInfrastructure();
        services.AddScreenMindWindowsPlatform();

        var settingsDescriptor = services.Single(x => x.ServiceType == typeof(ISettingsStore<AppSettings>));
        settingsDescriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
        settingsDescriptor.ImplementationFactory.Should().NotBeNull();

        var secretDescriptor = services.Single(x => x.ServiceType == typeof(ISecretStore));
        secretDescriptor.Lifetime.Should().Be(ServiceLifetime.Singleton);
        secretDescriptor.ImplementationType.Should().Be<WindowsCredentialSecretStore>();
    }
}
