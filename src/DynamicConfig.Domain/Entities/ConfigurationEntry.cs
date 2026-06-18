using DynamicConfig.Domain.Enums;

namespace DynamicConfig.Domain.Entities;

public class ConfigurationEntry
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ConfigurationValueType Type { get; set; }
    public string Value { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Monotonically increasing version counter. Incremented on every update.
    /// Used for version-based refresh: the cache only reloads when MaxVersion > last known version.
    /// </summary>
    public long Version { get; set; } = 1;
}
