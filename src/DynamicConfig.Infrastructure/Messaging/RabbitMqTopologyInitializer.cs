using RabbitMQ.Client;

namespace DynamicConfig.Infrastructure.Messaging;

public class RabbitMqTopologyInitializer
{
    private readonly RabbitMqConnectionFactory _connectionFactory;

    public RabbitMqTopologyInitializer(RabbitMqConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public void EnsureCreated()
    {
        using var connection = _connectionFactory.CreateConnection();
        using var channel = connection.CreateModel();
        var options = _connectionFactory.Options;

        channel.ExchangeDeclare(options.DeadLetterExchangeName, ExchangeType.Fanout, durable: true);
        channel.QueueDeclare(options.DeadLetterQueueName, durable: true, exclusive: false, autoDelete: false);
        channel.QueueBind(options.DeadLetterQueueName, options.DeadLetterExchangeName, string.Empty);

        var queueArgs = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = options.DeadLetterExchangeName
        };

        channel.ExchangeDeclare(options.ExchangeName, ExchangeType.Topic, durable: true);
        channel.QueueDeclare(options.QueueName, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
        channel.QueueBind(options.QueueName, options.ExchangeName, options.RoutingKey);
    }
}
