using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
        return services;
    }
}
