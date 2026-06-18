using DynamicConfig.Domain.Entities;
using DynamicConfig.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DynamicConfig.Infrastructure.Persistence;

public static class ConfigurationDbInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<ConfigurationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ConfigurationDbContext>>();

        try
        {
            await context.Database.MigrateAsync();
            await SeedAsync(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    private static async Task SeedAsync(ConfigurationDbContext context)
    {
        if (await context.ConfigurationEntries.AnyAsync())
        {
            return;
        }

        var seed = new[]
        {
            new ConfigurationEntry { Name = "SiteName",         Type = ConfigurationValueType.String,  Value = "soty.io", IsActive = true,  ApplicationName = "SERVICE-A", Version = 1 },
            new ConfigurationEntry { Name = "MaxItemCount",     Type = ConfigurationValueType.Int,     Value = "50",      IsActive = false, ApplicationName = "SERVICE-A", Version = 1 },
            new ConfigurationEntry { Name = "IsBasketEnabled",  Type = ConfigurationValueType.Bool,    Value = "1",       IsActive = true,  ApplicationName = "SERVICE-B", Version = 1 },
        };

        await context.ConfigurationEntries.AddRangeAsync(seed);
        await context.SaveChangesAsync();
    }
}
