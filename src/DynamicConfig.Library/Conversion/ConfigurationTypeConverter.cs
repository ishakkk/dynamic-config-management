using DynamicConfig.Domain.Entities;
using DynamicConfig.Domain.Enums;

namespace DynamicConfig.Library.Conversion;

public static class ConfigurationTypeConverter
{
    public static T Convert<T>(ConfigurationEntry entry)
    {
        var targetType = typeof(T);
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        object? converted = entry.Type switch
        {
            ConfigurationValueType.String => entry.Value,
            ConfigurationValueType.Int => int.Parse(entry.Value),
            ConfigurationValueType.Double => double.Parse(entry.Value, System.Globalization.CultureInfo.InvariantCulture),
            ConfigurationValueType.Bool => entry.Value is "1" or "true" or "True" or "TRUE",
            _ => throw new InvalidOperationException($"Unsupported configuration type: {entry.Type}")
        };

        if (converted is null)
        {
            throw new InvalidOperationException($"Configuration '{entry.Name}' has no value.");
        }

        return (T)System.Convert.ChangeType(converted, underlyingType, System.Globalization.CultureInfo.InvariantCulture)!;
    }
}
