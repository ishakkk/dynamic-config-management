using DynamicConfig.Library.Caching;
using DynamicConfig.Library.Conversion;
using DynamicConfig.Library.Storage;

namespace DynamicConfig.Library;

/// <summary>
/// Thread-safe configuration reader with version-based incremental refresh.
/// The timer first checks whether the max version in the database has advanced;
/// only then does it re-fetch all active entries, avoiding unnecessary database round-trips.
/// When storage is unavailable the last successful snapshot is served.
/// </summary>
public sealed class ConfigurationReader : IDisposable
{
    private readonly string _applicationName;
    private readonly IConfigurationStorage _storage;
    private readonly InMemoryConfigurationCache _cache = new();
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly Timer _refreshTimer;
    private volatile bool _disposed;

    public ConfigurationReader(string applicationName, string connectionString, int refreshTimerIntervalInMs)
    {
        if (string.IsNullOrWhiteSpace(applicationName))
            throw new ArgumentException("Application name is required.", nameof(applicationName));

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string is required.", nameof(connectionString));

        if (refreshTimerIntervalInMs <= 0)
            throw new ArgumentOutOfRangeException(nameof(refreshTimerIntervalInMs), "Refresh interval must be greater than zero.");

        _applicationName = applicationName;
        _storage = new SqlServerConfigurationStorage(connectionString);

        // Initial synchronous load
        RefreshAsync(CancellationToken.None).GetAwaiter().GetResult();

        _refreshTimer = new Timer(
            async _ => await VersionBasedRefreshAsync(CancellationToken.None).ConfigureAwait(false),
            null,
            refreshTimerIntervalInMs,
            refreshTimerIntervalInMs);
    }

    /// <summary>Gets a configuration value for the given key.</summary>
    public T GetValue<T>(string key)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Configuration key is required.", nameof(key));

        var snapshot = _cache.Snapshot();

        if (!snapshot.TryGetValue(key, out var entry))
            throw new KeyNotFoundException($"Configuration key '{key}' was not found for application '{_applicationName}'.");

        return ConfigurationTypeConverter.Convert<T>(entry);
    }

    /// <summary>Tries to get a configuration value; returns false if the key is absent.</summary>
    public bool TryGetValue<T>(string key, out T? value)
    {
        value = default;
        try
        {
            value = GetValue<T>(key);
            return true;
        }
        catch (KeyNotFoundException)
        {
            return false;
        }
    }

    // ──────────────────────────────────────────────────────────────
    // Version-based refresh: only do a full reload when the DB
    // reports a higher max version than we currently hold.
    // ──────────────────────────────────────────────────────────────

    internal async Task VersionBasedRefreshAsync(CancellationToken cancellationToken)
    {
        if (_disposed) return;

        if (!await _refreshLock.WaitAsync(0, cancellationToken).ConfigureAwait(false))
            return;

        try
        {
            var dbMaxVersion = await _storage
                .GetMaxVersionAsync(_applicationName, cancellationToken)
                .ConfigureAwait(false);

            // Nothing new in the DB – skip the full query
            if (dbMaxVersion <= _cache.MaxVersion && _cache.MaxVersion > 0)
                return;

            await DoFullRefreshAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Storage unavailable – keep the last successful snapshot
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    // Full (initial) refresh – always re-fetches everything
    internal async Task RefreshAsync(CancellationToken cancellationToken)
    {
        if (_disposed) return;

        if (!await _refreshLock.WaitAsync(0, cancellationToken).ConfigureAwait(false))
            return;

        try
        {
            await DoFullRefreshAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            // Storage unavailable – keep the last successful snapshot
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private async Task DoFullRefreshAsync(CancellationToken cancellationToken)
    {
        var entries = await _storage
            .LoadActiveConfigurationsAsync(_applicationName, cancellationToken)
            .ConfigureAwait(false);

        _cache.ReplaceAll(entries);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _disposed = true;
        _refreshTimer.Dispose();
        _refreshLock.Dispose();
    }
}
