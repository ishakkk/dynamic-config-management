using DynamicConfig.Domain.Enums;

namespace DynamicConfig.Domain.DTOs;

public class UpdateConfigurationEntryRequest
{
    public string Name { get; set; } = string.Empty;
    public ConfigurationValueType Type { get; set; }
    public string Value { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
}
