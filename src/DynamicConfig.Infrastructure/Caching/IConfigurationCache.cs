using DynamicConfig.Domain.Entities;

namespace DynamicConfig.Infrastructure.Caching;

public interface IConfigurationCache
{
    Task<IReadOnlyList<ConfigurationEntry>?> GetAsync(string applicationName, CancellationToken cancellationToken = default);
    Task SetAsync(string applicationName, IReadOnlyList<ConfigurationEntry> entries, CancellationToken cancellationToken = default);
    Task InvalidateAsync(string applicationName, CancellationToken cancellationToken = default);
}
