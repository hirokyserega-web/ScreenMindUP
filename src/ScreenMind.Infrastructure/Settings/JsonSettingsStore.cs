using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using ScreenMind.Core.Abstractions;
using ScreenMind.Core.Settings;

namespace ScreenMind.Infrastructure.Settings;

/// <summary>
/// JSON-file settings store with schema migration, atomic writes,
/// a rolling .bak backup and recovery from corrupted files. Never stores secrets.
/// </summary>
public sealed class JsonSettingsStore : ISettingsStore<AppSettings>, IDisposable
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly string _filePath;
    private readonly string _backupPath;
    private readonly ILogger<JsonSettingsStore> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public JsonSettingsStore(string filePath, ILogger<JsonSettingsStore> logger)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
        _backupPath = filePath + ".bak";
        _logger = logger;
    }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            CleanupStaleTempFiles();
            var primaryExists = File.Exists(_filePath);
            var settings = await TryLoadFileAsync(_filePath, cancellationToken).ConfigureAwait(false);
            if (settings is not null)
            {
                return settings;
            }

            settings = await TryLoadFileAsync(_backupPath, cancellationToken).ConfigureAwait(false);
            if (settings is not null)
            {
                _logger.LogWarning("Primary settings file was missing or corrupted; recovered from backup.");
                if (primaryExists)
                {
                    QuarantineCorruptedFile();
                }

                await WritePrimaryWithoutRotatingBackupAsync(settings, cancellationToken).ConfigureAwait(false);
                return settings;
            }

            if (primaryExists)
            {
                QuarantineCorruptedFile();
            }

            var defaults = new AppSettings();
            await SaveCoreAsync(defaults, cancellationToken).ConfigureAwait(false);
            return defaults;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        SettingsValidator.EnsureValid(settings);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            CleanupStaleTempFiles();
            await SaveCoreAsync(settings, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();

    private async Task<AppSettings?> TryLoadFileAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false);
            if (JsonNode.Parse(json) is not JsonObject root)
            {
                return null;
            }

            var (migratedNode, migrated) = SettingsMigrator.Migrate(root);
            var settings = migratedNode.Deserialize<AppSettings>(_jsonOptions);
            if (settings is null)
            {
                return null;
            }

            SettingsValidator.EnsureValid(settings);

            if (migrated)
            {
                _logger.LogInformation(
                    "Settings migrated to schema version {Version}.", AppSettings.CurrentSchemaVersion);
            }

            return settings;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "Failed to load settings from {Path}: {ErrorType}.",
                path,
                ex.GetType().Name);
            return null;
        }
    }

    private async Task SaveCoreAsync(AppSettings settings, CancellationToken cancellationToken)
    {
        var tempPath = await WriteTempFileAsync(settings, cancellationToken).ConfigureAwait(false);
        try
        {
            if (File.Exists(_filePath))
            {
                File.Copy(_filePath, _backupPath, overwrite: true);
            }

            File.Move(tempPath, _filePath, overwrite: true);
        }
        finally
        {
            DeleteIfExists(tempPath);
        }
    }

    private async Task WritePrimaryWithoutRotatingBackupAsync(
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        var tempPath = await WriteTempFileAsync(settings, cancellationToken).ConfigureAwait(false);
        try
        {
            File.Move(tempPath, _filePath, overwrite: true);
        }
        finally
        {
            DeleteIfExists(tempPath);
        }
    }

    private async Task<string> WriteTempFileAsync(
        AppSettings settings,
        CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, _jsonOptions);
        var tempPath = _filePath + ".tmp-" + Guid.NewGuid().ToString("N");
        await File.WriteAllTextAsync(tempPath, json, cancellationToken).ConfigureAwait(false);
        return tempPath;
    }

    private void CleanupStaleTempFiles()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
        {
            return;
        }

        var prefix = Path.GetFileName(_filePath) + ".tmp";
        foreach (var path in Directory.EnumerateFiles(directory, prefix + "*"))
        {
            DeleteIfExists(path);
        }
    }

    private void QuarantineCorruptedFile()
    {
        try
        {
            var quarantinePath = _filePath + $".corrupt-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Guid.NewGuid():N}";
            File.Move(_filePath, quarantinePath, overwrite: false);
            _logger.LogWarning("Corrupted settings file moved to {Path}.", quarantinePath);
        }
        catch (IOException)
        {
            _logger.LogError("Failed to quarantine corrupted settings file.");
        }
    }

    private void DeleteIfExists(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
            _logger.LogWarning("Failed to delete temporary settings file {Path}.", path);
        }
    }
}
