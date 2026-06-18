namespace DynamicConfig.Infrastructure.Messaging;

public class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string ExchangeName { get; set; } = "dynamic-config.exchange";
    public string QueueName { get; set; } = "dynamic-config.queue";
    public string DeadLetterExchangeName { get; set; } = "dynamic-config.dlx";
    public string DeadLetterQueueName { get; set; } = "dynamic-config.dlq";
    public string RoutingKey { get; set; } = "config.changed";
}
