using DynamicConfig.Domain.Events;

namespace DynamicConfig.Domain.Interfaces;

public interface IConfigurationMessagePublisher
{
    Task PublishAsync(ConfigurationChangedEvent changedEvent, CancellationToken cancellationToken = default);
}
