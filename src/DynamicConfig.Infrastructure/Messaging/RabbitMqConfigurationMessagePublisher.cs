using System.Text;
using System.Text.Json;
using DynamicConfig.Domain.Events;
using DynamicConfig.Domain.Interfaces;
using RabbitMQ.Client;

namespace DynamicConfig.Infrastructure.Messaging;

public class RabbitMqConfigurationMessagePublisher : IConfigurationMessagePublisher, IDisposable
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly IConnection _connection;
    private readonly IModel _channel;

    public RabbitMqConfigurationMessagePublisher(RabbitMqConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
        _connection = _connectionFactory.CreateConnection();
        _channel = _connection.CreateModel();
    }

    public Task PublishAsync(ConfigurationChangedEvent changedEvent, CancellationToken cancellationToken = default)
    {
        var options = _connectionFactory.Options;
        var payload = JsonSerializer.Serialize(changedEvent);
        var body = Encoding.UTF8.GetBytes(payload);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true;
        properties.ContentType = "application/json";
        properties.MessageId = Guid.NewGuid().ToString();
        properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

        _channel.BasicPublish(
            exchange: options.ExchangeName,
            routingKey: options.RoutingKey,
            basicProperties: properties,
            body: body);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel.Dispose();
        _connection.Dispose();
    }
}
