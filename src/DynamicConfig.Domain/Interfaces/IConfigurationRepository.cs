using DynamicConfig.Domain.Entities;

namespace DynamicConfig.Domain.Interfaces;

public interface IConfigurationRepository
{
    Task<IReadOnlyList<ConfigurationEntry>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ConfigurationEntry>> GetByApplicationAsync(string applicationName, CancellationToken cancellationToken = default);
    Task<ConfigurationEntry?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ConfigurationEntry> AddAsync(ConfigurationEntry entry, CancellationToken cancellationToken = default);
    Task<ConfigurationEntry?> UpdateAsync(ConfigurationEntry entry, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns only entries whose Version > sinceVersion for the given application.
    /// Used for version-based incremental refresh.
    /// </summary>
    Task<IReadOnlyList<ConfigurationEntry>> GetChangedSinceVersionAsync(
        string applicationName,
        long sinceVersion,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the maximum Version value for active entries of the given application.
    /// Returns 0 if there are no entries.
    /// </summary>
    Task<long> GetMaxVersionAsync(string applicationName, CancellationToken cancellationToken = default);
}
