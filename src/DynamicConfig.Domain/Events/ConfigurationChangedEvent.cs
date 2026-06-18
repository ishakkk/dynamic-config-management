namespace DynamicConfig.Domain.Events;

public class ConfigurationChangedEvent
{
    public int ConfigurationId { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ChangeType { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
