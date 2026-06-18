using DynamicConfig.Domain.DTOs;
using DynamicConfig.Domain.Entities;
using DynamicConfig.Domain.Events;
using DynamicConfig.Domain.Interfaces;
using DynamicConfig.Infrastructure.Caching;
using Microsoft.Extensions.Logging;

namespace DynamicConfig.Infrastructure.Services;

public class ConfigurationService
{
    private readonly IConfigurationRepository _repository;
    private readonly IConfigurationMessagePublisher _publisher;
    private readonly IConfigurationCache _cache;
    private readonly ILogger<ConfigurationService> _logger;

    public ConfigurationService(
        IConfigurationRepository repository,
        IConfigurationMessagePublisher publisher,
        IConfigurationCache cache,
        ILogger<ConfigurationService> logger)
    {
        _repository = repository;
        _publisher = publisher;
        _cache = cache;
        _logger = logger;
    }

    public Task<IReadOnlyList<ConfigurationEntry>> GetAllAsync(CancellationToken cancellationToken = default)
        => _repository.GetAllAsync(cancellationToken);

    public async Task<IReadOnlyList<ConfigurationEntry>> GetByApplicationAsync(
        string applicationName,
        CancellationToken cancellationToken = default)
    {
        var cached = await _cache.GetAsync(applicationName, cancellationToken);
        if (cached is not null)
        {
            _logger.LogDebug("Cache hit for application {ApplicationName}", applicationName);
            return cached;
        }

        var entries = await _repository.GetByApplicationAsync(applicationName, cancellationToken);
        await _cache.SetAsync(applicationName, entries, cancellationToken);
        return entries;
    }

    public async Task<ConfigurationEntry> CreateAsync(
        CreateConfigurationEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var entry = new ConfigurationEntry
        {
            Name = request.Name,
            Type = request.Type,
            Value = request.Value,
            IsActive = request.IsActive,
            ApplicationName = request.ApplicationName
        };

        var created = await _repository.AddAsync(entry, cancellationToken);
        await _cache.InvalidateAsync(created.ApplicationName, cancellationToken);

        await PublishEventAsync(created, "Created", cancellationToken);

        return created;
    }

    public async Task<ConfigurationEntry?> UpdateAsync(
        int id,
        UpdateConfigurationEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        var entry = new ConfigurationEntry
        {
            Id = id,
            Name = request.Name,
            Type = request.Type,
            Value = request.Value,
            IsActive = request.IsActive,
            ApplicationName = request.ApplicationName
        };

        var updated = await _repository.UpdateAsync(entry, cancellationToken);
        if (updated is null)
        {
            return null;
        }

        await _cache.InvalidateAsync(updated.ApplicationName, cancellationToken);
        await PublishEventAsync(updated, "Updated", cancellationToken);

        return updated;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        var deleted = await _repository.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return false;
        }

        await _cache.InvalidateAsync(existing.ApplicationName, cancellationToken);
        await PublishEventAsync(existing, "Deleted", cancellationToken);

        return true;
    }

    private async Task PublishEventAsync(ConfigurationEntry entry, string changeType, CancellationToken cancellationToken)
    {
        try
        {
            await _publisher.PublishAsync(new ConfigurationChangedEvent
            {
                ConfigurationId = entry.Id,
                ApplicationName = entry.ApplicationName,
                Name = entry.Name,
                ChangeType = changeType
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish {ChangeType} event for {ApplicationName}/{Name}",
                changeType, entry.ApplicationName, entry.Name);
        }
    }
}
