using DynamicConfig.Domain.Entities;

namespace DynamicConfig.Library.Storage;

internal interface IConfigurationStorage
{
    Task<IReadOnlyList<ConfigurationEntry>> LoadActiveConfigurationsAsync(
        string applicationName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the max Version value for active entries of the given application.
    /// 0 means no entries exist.
    /// </summary>
    Task<long> GetMaxVersionAsync(string applicationName, CancellationToken cancellationToken = default);
}
