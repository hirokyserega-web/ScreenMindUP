using Microsoft.Extensions.DependencyInjection;
using ScreenMind.Core.Abstractions;
using ScreenMind.Platform.Windows.Security;

namespace ScreenMind.Platform.Windows.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddScreenMindWindowsPlatform(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<ISecretStore, WindowsCredentialSecretStore>();
        return services;
    }
}
