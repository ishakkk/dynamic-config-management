using System.Text.Json;
using DynamicConfig.Domain.Entities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace DynamicConfig.Infrastructure.Caching;

public class RedisConfigurationCache : IConfigurationCache
{
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromMinutes(5);
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<RedisConfigurationCache> _logger;

    public RedisConfigurationCache(IDistributedCache distributedCache, ILogger<RedisConfigurationCache> logger)
    {
        _distributedCache = distributedCache;
        _logger = logger;
    }

    private static string CacheKey(string applicationName) => $"dynconf:{applicationName}";

    public async Task<IReadOnlyList<ConfigurationEntry>?> GetAsync(string applicationName, CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = await _distributedCache.GetAsync(CacheKey(applicationName), cancellationToken);
            if (bytes is null)
            {
                return null;
            }

            return JsonSerializer.Deserialize<List<ConfigurationEntry>>(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache get failed for {ApplicationName}. Falling back to database.", applicationName);
            return null;
        }
    }

    public async Task SetAsync(string applicationName, IReadOnlyList<ConfigurationEntry> entries, CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(entries);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = DefaultExpiry
            };
            await _distributedCache.SetAsync(CacheKey(applicationName), bytes, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache set failed for {ApplicationName}. Continuing without caching.", applicationName);
        }
    }

    public async Task InvalidateAsync(string applicationName, CancellationToken cancellationToken = default)
    {
        try
        {
            await _distributedCache.RemoveAsync(CacheKey(applicationName), cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis cache invalidation failed for {ApplicationName}.", applicationName);
        }
    }
}
