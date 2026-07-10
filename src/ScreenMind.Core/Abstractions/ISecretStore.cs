namespace ScreenMind.Core.Abstractions;

/// <summary>
/// OS-backed secret storage (Windows Credential Manager / DPAPI). Secrets never
/// travel through JSON settings, source code or logs, and are never returned to
/// the UI once written.
/// </summary>
public interface ISecretStore
{
    Task SetSecretAsync(string key, string value, CancellationToken cancellationToken);

    /// <summary>Reads a secret for internal use (e.g. an HTTP request). Null when absent.</summary>
    Task<string?> GetSecretAsync(string key, CancellationToken cancellationToken);

    Task<bool> HasSecretAsync(string key, CancellationToken cancellationToken);

    Task RemoveSecretAsync(string key, CancellationToken cancellationToken);
}
