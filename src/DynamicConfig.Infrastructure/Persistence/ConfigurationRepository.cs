using DynamicConfig.Domain.Entities;
using DynamicConfig.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Retry;

namespace DynamicConfig.Infrastructure.Persistence;

public class ConfigurationRepository : IConfigurationRepository
{
    private readonly ConfigurationDbContext _dbContext;
    private readonly ResiliencePipeline _retryPipeline;

    public ConfigurationRepository(ConfigurationDbContext dbContext)
    {
        _dbContext = dbContext;
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

    public async Task<IReadOnlyList<ConfigurationEntry>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _retryPipeline.ExecuteAsync(async _ =>
        {
            var items = await _dbContext.ConfigurationEntries
                .AsNoTracking()
                .OrderBy(x => x.ApplicationName)
                .ThenBy(x => x.Name)
                .ToListAsync(cancellationToken);

            return (IReadOnlyList<ConfigurationEntry>)items;
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<ConfigurationEntry>> GetByApplicationAsync(
        string applicationName,
        CancellationToken cancellationToken = default)
    {
        return await _retryPipeline.ExecuteAsync(async _ =>
        {
            var items = await _dbContext.ConfigurationEntries
                .AsNoTracking()
                .Where(x => x.ApplicationName == applicationName)
                .OrderBy(x => x.Name)
                .ToListAsync(cancellationToken);

            return (IReadOnlyList<ConfigurationEntry>)items;
        }, cancellationToken);
    }

    public async Task<ConfigurationEntry?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _retryPipeline.ExecuteAsync(async _ =>
            await _dbContext.ConfigurationEntries.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken),
            cancellationToken);
    }

    public async Task<ConfigurationEntry> AddAsync(ConfigurationEntry entry, CancellationToken cancellationToken = default)
    {
        entry.UpdatedAt = DateTime.UtcNow;
        entry.Version = 1;
        _dbContext.ConfigurationEntries.Add(entry);

        await _retryPipeline.ExecuteAsync(
            async _ => await _dbContext.SaveChangesAsync(cancellationToken),
            cancellationToken);

        return entry;
    }

    public async Task<ConfigurationEntry?> UpdateAsync(ConfigurationEntry entry, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.ConfigurationEntries.FirstOrDefaultAsync(x => x.Id == entry.Id, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        existing.Name = entry.Name;
        existing.Type = entry.Type;
        existing.Value = entry.Value;
        existing.IsActive = entry.IsActive;
        existing.ApplicationName = entry.ApplicationName;
        existing.UpdatedAt = DateTime.UtcNow;
        existing.Version = existing.Version + 1; // Version-based refresh

        await _retryPipeline.ExecuteAsync(
            async _ => await _dbContext.SaveChangesAsync(cancellationToken),
            cancellationToken);

        return existing;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.ConfigurationEntries.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        _dbContext.ConfigurationEntries.Remove(existing);

        await _retryPipeline.ExecuteAsync(
            async _ => await _dbContext.SaveChangesAsync(cancellationToken),
            cancellationToken);

        return true;
    }

    public async Task<IReadOnlyList<ConfigurationEntry>> GetChangedSinceVersionAsync(
        string applicationName,
        long sinceVersion,
        CancellationToken cancellationToken = default)
    {
        return await _retryPipeline.ExecuteAsync(async _ =>
        {
            var items = await _dbContext.ConfigurationEntries
                .AsNoTracking()
                .Where(x => x.ApplicationName == applicationName && x.Version > sinceVersion)
                .OrderBy(x => x.Version)
                .ToListAsync(cancellationToken);

            return (IReadOnlyList<ConfigurationEntry>)items;
        }, cancellationToken);
    }

    public async Task<long> GetMaxVersionAsync(string applicationName, CancellationToken cancellationToken = default)
    {
        return await _retryPipeline.ExecuteAsync(async _ =>
        {
            var max = await _dbContext.ConfigurationEntries
                .AsNoTracking()
                .Where(x => x.ApplicationName == applicationName && x.IsActive)
                .MaxAsync(x => (long?)x.Version, cancellationToken);

            return max ?? 0L;
        }, cancellationToken);
    }
}
