using DynamicConfig.Domain.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace DynamicConfig.Infrastructure.Messaging;

public class ConfigurationChangeConsumerService : BackgroundService
{
    private readonly RabbitMqConnectionFactory _connectionFactory;
    private readonly ILogger<ConfigurationChangeConsumerService> _logger;
    private IConnection? _connection;
    private IModel? _channel;

    public ConfigurationChangeConsumerService(
        RabbitMqConnectionFactory connectionFactory,
        ILogger<ConfigurationChangeConsumerService> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var topology = new RabbitMqTopologyInitializer(_connectionFactory);
        topology.EnsureCreated();

        _connection = _connectionFactory.CreateConnection();
        _channel = _connection.CreateModel();

        var options = _connectionFactory.Options;
        _channel.BasicQos(0, 1, false);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.Received += async (_, eventArgs) =>
        {
            try
            {
                var json = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
                var changedEvent = JsonSerializer.Deserialize<ConfigurationChangedEvent>(json);

                if (changedEvent is null)
                    throw new InvalidOperationException("Invalid payload");

                _logger.LogInformation("Processed {Name}", changedEvent.Name);

                _channel.BasicAck(eventArgs.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Processing failed");
                _channel.BasicNack(eventArgs.DeliveryTag, false, false);
            }

            await Task.CompletedTask;
        };

        _ = _channel.BasicConsume(options.QueueName, false, consumer);


        return Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
        base.Dispose();
    }
}
