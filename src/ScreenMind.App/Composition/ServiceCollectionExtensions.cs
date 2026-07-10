using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScreenMind.Infrastructure.DependencyInjection;
using ScreenMind.Platform.Windows.DependencyInjection;

namespace ScreenMind.App.Composition;

/// <summary>
/// Root composition of the application. Individual subsystems register
/// their services here as they are implemented in subsequent phases.
/// </summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddScreenMind(this IServiceCollection services)
    {
        services.AddLogging(builder => builder.AddConsole());
        services.AddScreenMindInfrastructure();
        services.AddScreenMindWindowsPlatform();
        return services;
    }
}
