using DynamicConfig.Domain.Entities;
using DynamicConfig.Domain.Enums;
using DynamicConfig.Library.Conversion;
using FluentAssertions;

namespace DynamicConfig.UnitTests;

public class ConfigurationTypeConverterTests
{
    [Fact]
    public void Convert_ShouldReturnString()
    {
        var entry = CreateEntry(ConfigurationValueType.String, "soty.io");
        ConfigurationTypeConverter.Convert<string>(entry).Should().Be("soty.io");
    }

    [Fact]
    public void Convert_ShouldReturnInt()
    {
        var entry = CreateEntry(ConfigurationValueType.Int, "50");
        ConfigurationTypeConverter.Convert<int>(entry).Should().Be(50);
    }

    [Fact]
    public void Convert_ShouldReturnDouble()
    {
        var entry = CreateEntry(ConfigurationValueType.Double, "12.5");
        ConfigurationTypeConverter.Convert<double>(entry).Should().Be(12.5);
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("0", false)]
    public void Convert_ShouldReturnBool(string rawValue, bool expected)
    {
        var entry = CreateEntry(ConfigurationValueType.Bool, rawValue);
        ConfigurationTypeConverter.Convert<bool>(entry).Should().Be(expected);
    }

    private static ConfigurationEntry CreateEntry(ConfigurationValueType type, string value) =>
        new()
        {
            Name = "Test",
            Type = type,
            Value = value,
            IsActive = true,
            ApplicationName = "SERVICE-A"
        };
}
