using DynamicConfig.Domain.Entities;
using DynamicConfig.Domain.Enums;
using DynamicConfig.Library.Caching;
using FluentAssertions;

namespace DynamicConfig.UnitTests;

public class InMemoryConfigurationCacheTests
{
    private static ConfigurationEntry MakeEntry(string name, string value, long version = 1) =>
        new()
        {
            Name = name,
            Value = value,
            Type = ConfigurationValueType.String,
            IsActive = true,
            ApplicationName = "TEST",
            Version = version
        };

    [Fact]
    public void ReplaceAll_ShouldUpdateSnapshot()
    {
        var cache = new InMemoryConfigurationCache();
        var entries = new List<ConfigurationEntry> { MakeEntry("Key1", "val1") };

        cache.ReplaceAll(entries);

        var snapshot = cache.Snapshot();
        snapshot.Should().ContainKey("Key1");
        snapshot["Key1"].Value.Should().Be("val1");
    }

    [Fact]
    public void ReplaceAll_ShouldUpdateMaxVersion()
    {
        var cache = new InMemoryConfigurationCache();
        cache.ReplaceAll(new[] { MakeEntry("A", "a", 5), MakeEntry("B", "b", 3) });

        cache.MaxVersion.Should().Be(5);
    }

    [Fact]
    public void HasChanges_WhenValueChanged_ShouldReturnTrue()
    {
        var cache = new InMemoryConfigurationCache();
        cache.ReplaceAll(new[] { MakeEntry("Key1", "old", 1) });

        var incoming = new List<ConfigurationEntry> { MakeEntry("Key1", "new", 2) };
        cache.HasChanges(incoming).Should().BeTrue();
    }

    [Fact]
    public void HasChanges_WhenVersionChanged_ShouldReturnTrue()
    {
        var cache = new InMemoryConfigurationCache();
        cache.ReplaceAll(new[] { MakeEntry("Key1", "same", 1) });

        var incoming = new List<ConfigurationEntry> { MakeEntry("Key1", "same", 2) };
        cache.HasChanges(incoming).Should().BeTrue();
    }

    [Fact]
    public void HasChanges_WhenNothingChanged_ShouldReturnFalse()
    {
        var cache = new InMemoryConfigurationCache();
        cache.ReplaceAll(new[] { MakeEntry("Key1", "val", 1) });

        var incoming = new List<ConfigurationEntry> { MakeEntry("Key1", "val", 1) };
        cache.HasChanges(incoming).Should().BeFalse();
    }

    [Fact]
    public void MergeChanges_ShouldAddNewEntry()
    {
        var cache = new InMemoryConfigurationCache();
        cache.ReplaceAll(new[] { MakeEntry("Existing", "v", 1) });

        cache.MergeChanges(new[] { MakeEntry("NewKey", "newVal", 2) });

        var snapshot = cache.Snapshot();
        snapshot.Should().ContainKey("NewKey");
        snapshot["NewKey"].Value.Should().Be("newVal");
        cache.MaxVersion.Should().Be(2);
    }

    [Fact]
    public void MergeChanges_InactiveEntry_ShouldBeRemoved()
    {
        var cache = new InMemoryConfigurationCache();
        cache.ReplaceAll(new[] { MakeEntry("ToRemove", "v", 1) });

        var inactive = new ConfigurationEntry
        {
            Name = "ToRemove",
            Value = "v",
            IsActive = false,
            Type = ConfigurationValueType.String,
            ApplicationName = "TEST",
            Version = 2
        };
        cache.MergeChanges(new[] { inactive });

        cache.Snapshot().Should().NotContainKey("ToRemove");
    }

    [Fact]
    public void Snapshot_IsIsolatedFromFutureChanges()
    {
        var cache = new InMemoryConfigurationCache();
        cache.ReplaceAll(new[] { MakeEntry("K", "v1", 1) });

        var snap1 = cache.Snapshot();
        cache.ReplaceAll(new[] { MakeEntry("K", "v2", 2) });

        // snap1 should still hold the old value
        snap1["K"].Value.Should().Be("v1");
    }
}
