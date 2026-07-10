using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScreenMind.Core.Abstractions;
using ScreenMind.Core.Settings;
using ScreenMind.Infrastructure.Settings;

namespace ScreenMind.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddScreenMindInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var settingsFilePath = Path.Combine(localAppData, "ScreenMind", "settings.json");
        return services.AddScreenMindInfrastructure(settingsFilePath);
    }

    public static IServiceCollection AddScreenMindInfrastructure(
        this IServiceCollection services,
        string settingsFilePath)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(settingsFilePath);

        services.AddSingleton<ISettingsStore<AppSettings>>(provider =>
            new JsonSettingsStore(
                settingsFilePath,
                provider.GetRequiredService<ILogger<JsonSettingsStore>>()));
        return services;
    }
}
