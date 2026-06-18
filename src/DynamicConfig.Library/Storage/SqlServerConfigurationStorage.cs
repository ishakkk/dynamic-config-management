using DynamicConfig.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;

namespace DynamicConfig.Library.Storage;

internal sealed class SqlServerConfigurationStorage : IConfigurationStorage
{
    private readonly string _connectionString;
    private readonly ResiliencePipeline _retryPipeline;

    public SqlServerConfigurationStorage(string connectionString)
    {
        _connectionString = connectionString;
        _retryPipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions
            {
                MaxRetryAttempts = 3,
                Delay = TimeSpan.FromMilliseconds(200),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true
            })
            .Build();
    }

    public async Task<IReadOnlyList<ConfigurationEntry>> LoadActiveConfigurationsAsync(
        string applicationName,
        CancellationToken cancellationToken = default)
    {
        return await _retryPipeline.ExecuteAsync(async _ =>
        {
            await using var context = CreateContext();

            return await context.ConfigurationEntries
                .AsNoTracking()
                .Where(x => x.ApplicationName == applicationName && x.IsActive)
                .ToListAsync(cancellationToken);
        }, cancellationToken);
    }

    public async Task<long> GetMaxVersionAsync(string applicationName, CancellationToken cancellationToken = default)
    {
        return await _retryPipeline.ExecuteAsync(async _ =>
        {
            await using var context = CreateContext();

            var max = await context.ConfigurationEntries
                .AsNoTracking()
                .Where(x => x.ApplicationName == applicationName && x.IsActive)
                .MaxAsync(x => (long?)x.Version, cancellationToken);

            return max ?? 0L;
        }, cancellationToken);
    }

    private ConfigurationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ConfigurationDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        return new ConfigurationDbContext(options);
    }
}
