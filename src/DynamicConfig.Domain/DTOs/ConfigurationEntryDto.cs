using DynamicConfig.Domain.Enums;

namespace DynamicConfig.Domain.DTOs;

public sealed class ConfigurationEntryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ConfigurationValueType Type { get; set; }
    public string Value { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public long Version { get; set; }
}
