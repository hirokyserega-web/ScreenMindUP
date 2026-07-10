namespace ScreenMind.Core.Abstractions;

/// <summary>
/// Typed, versioned settings persistence with atomic writes, backup and
/// recovery from corrupted files.
/// </summary>
public interface ISettingsStore<TSettings>
    where TSettings : class
{
    Task<TSettings> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(TSettings settings, CancellationToken cancellationToken);
}
