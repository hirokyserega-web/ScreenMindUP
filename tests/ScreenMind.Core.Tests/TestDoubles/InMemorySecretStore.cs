using System.Collections.Concurrent;
using ScreenMind.Core.Abstractions;

namespace ScreenMind.Core.Tests.TestDoubles;

internal sealed class InMemorySecretStore : ISecretStore
{
    private readonly ConcurrentDictionary<string, string> _secrets = new(StringComparer.Ordinal);

    public Task SetSecretAsync(string key, string value, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);
        _secrets[key] = value;
        return Task.CompletedTask;
    }

    public Task<string?> GetSecretAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return Task.FromResult(_secrets.TryGetValue(key, out var value) ? value : null);
    }

    public async Task<bool> HasSecretAsync(string key, CancellationToken cancellationToken) =>
        await GetSecretAsync(key, cancellationToken) is not null;

    public Task RemoveSecretAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        _secrets.TryRemove(key, out _);
        return Task.CompletedTask;
    }
}
