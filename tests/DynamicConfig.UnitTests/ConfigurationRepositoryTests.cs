using DynamicConfig.Domain.Entities;
using DynamicConfig.Domain.Enums;
using DynamicConfig.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace DynamicConfig.UnitTests;

public class ConfigurationRepositoryTests
{
    private static ConfigurationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ConfigurationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ConfigurationDbContext(options);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistEntry()
    {
        await using var ctx = CreateContext();
        var repo = new ConfigurationRepository(ctx);

        var entry = new ConfigurationEntry
        {
            Name = "SiteName",
            Type = ConfigurationValueType.String,
            Value = "soty.io",
            IsActive = true,
            ApplicationName = "SERVICE-A"
        };

        var created = await repo.AddAsync(entry);

        created.Id.Should().BeGreaterThan(0);
        created.Version.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_ShouldIncrementVersion()
    {
        await using var ctx = CreateContext();
        var repo = new ConfigurationRepository(ctx);

        var entry = new ConfigurationEntry
        {
            Name = "Counter",
            Type = ConfigurationValueType.Int,
            Value = "10",
            IsActive = true,
            ApplicationName = "SERVICE-A"
        };

        var created = await repo.AddAsync(entry);
        created.Version.Should().Be(1);

        var update = new ConfigurationEntry
        {
            Id = created.Id,
            Name = "Counter",
            Type = ConfigurationValueType.Int,
            Value = "20",
            IsActive = true,
            ApplicationName = "SERVICE-A"
        };

        var updated = await repo.UpdateAsync(update);
        updated.Should().NotBeNull();
        updated!.Value.Should().Be("20");
        updated.Version.Should().Be(2);
    }

    [Fact]
    public async Task GetByApplicationAsync_ShouldReturnOnlyOwnEntries()
    {
        await using var ctx = CreateContext();
        var repo = new ConfigurationRepository(ctx);

        await repo.AddAsync(new ConfigurationEntry { Name = "A", Type = ConfigurationValueType.String, Value = "v", IsActive = true, ApplicationName = "SERVICE-A" });
        await repo.AddAsync(new ConfigurationEntry { Name = "B", Type = ConfigurationValueType.String, Value = "v", IsActive = true, ApplicationName = "SERVICE-B" });

        var entries = await repo.GetByApplicationAsync("SERVICE-A");

        entries.Should().HaveCount(1);
        entries[0].ApplicationName.Should().Be("SERVICE-A");
    }

    [Fact]
    public async Task GetMaxVersionAsync_ShouldReturnHighestVersion()
    {
        await using var ctx = CreateContext();
        var repo = new ConfigurationRepository(ctx);

        await repo.AddAsync(new ConfigurationEntry { Name = "X", Type = ConfigurationValueType.String, Value = "v", IsActive = true, ApplicationName = "APP" });
        var e2 = await repo.AddAsync(new ConfigurationEntry { Name = "Y", Type = ConfigurationValueType.String, Value = "v", IsActive = true, ApplicationName = "APP" });

        // Update Y to bump version to 2
        await repo.UpdateAsync(new ConfigurationEntry { Id = e2.Id, Name = "Y", Type = ConfigurationValueType.String, Value = "v2", IsActive = true, ApplicationName = "APP" });

        var maxVersion = await repo.GetMaxVersionAsync("APP");
        maxVersion.Should().Be(2);
    }

    [Fact]
    public async Task GetChangedSinceVersionAsync_ShouldReturnOnlyNewerEntries()
    {
        await using var ctx = CreateContext();
        var repo = new ConfigurationRepository(ctx);

        var e1 = await repo.AddAsync(new ConfigurationEntry { Name = "A", Type = ConfigurationValueType.String, Value = "v", IsActive = true, ApplicationName = "APP" });
        // e1.Version = 1

        // Update to get version 2
        await repo.UpdateAsync(new ConfigurationEntry { Id = e1.Id, Name = "A", Type = ConfigurationValueType.String, Value = "v2", IsActive = true, ApplicationName = "APP" });

        var changedSinceV1 = await repo.GetChangedSinceVersionAsync("APP", sinceVersion: 1);
        changedSinceV1.Should().HaveCount(1);
        changedSinceV1[0].Version.Should().Be(2);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveEntry()
    {
        await using var ctx = CreateContext();
        var repo = new ConfigurationRepository(ctx);

        var created = await repo.AddAsync(new ConfigurationEntry
        {
            Name = "ToDelete",
            Type = ConfigurationValueType.String,
            Value = "v",
            IsActive = true,
            ApplicationName = "APP"
        });

        var deleted = await repo.DeleteAsync(created.Id);
        deleted.Should().BeTrue();

        var found = await repo.GetByIdAsync(created.Id);
        found.Should().BeNull();
    }
}
