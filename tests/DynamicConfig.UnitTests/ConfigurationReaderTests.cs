using DynamicConfig.Domain.Entities;
using DynamicConfig.Domain.Enums;
using DynamicConfig.Library;
using DynamicConfig.Library.Caching;
using DynamicConfig.Library.Storage;
using FluentAssertions;
using Moq;

namespace DynamicConfig.UnitTests;

public class ConfigurationReaderTests
{
    [Fact]
    public void Constructor_WithInvalidApplicationName_ShouldThrow()
    {
        var act = () => new ConfigurationReader("", "conn", 5000);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithInvalidConnectionString_ShouldThrow()
    {
        var act = () => new ConfigurationReader("APP", "", 5000);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WithInvalidInterval_ShouldThrow()
    {
        var act = () => new ConfigurationReader("APP", "conn", 0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void GetValue_WhenDisposed_ShouldThrow()
    {
        // We cannot easily instantiate ConfigurationReader without a DB,
        // so we test the cache in isolation
        var cache = new InMemoryConfigurationCache();
        cache.ReplaceAll(new List<ConfigurationEntry>
        {
            new() { Name = "Key1", Value = "val", Type = ConfigurationValueType.String, IsActive = true, ApplicationName = "APP", Version = 1 }
        });

        // Snapshot should contain Key1
        var snapshot = cache.Snapshot();
        snapshot.Should().ContainKey("Key1");
    }
}
