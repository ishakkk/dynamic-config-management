using DynamicConfig.Domain.Entities;
using DynamicConfig.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace DynamicConfig.Library.Storage;

internal sealed class ConfigurationDbContext : DbContext
{
    public ConfigurationDbContext(DbContextOptions<ConfigurationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ConfigurationEntry> ConfigurationEntries => Set<ConfigurationEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConfigurationEntry>(entity =>
        {
            entity.ToTable("ConfigurationEntries");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Value).HasMaxLength(2000).IsRequired();
            entity.Property(x => x.ApplicationName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Type).HasConversion<int>();
            entity.HasIndex(x => new { x.ApplicationName, x.Name }).IsUnique();
        });
    }
}
