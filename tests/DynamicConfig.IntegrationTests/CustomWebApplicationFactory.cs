using DynamicConfig.Domain.Interfaces;
using DynamicConfig.Infrastructure.Caching;
using DynamicConfig.Infrastructure.Messaging;
using DynamicConfig.Infrastructure.Persistence;
using DynamicConfig.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DynamicConfig.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove real infrastructure
            services.RemoveAll(typeof(DbContextOptions<ConfigurationDbContext>));
            services.RemoveAll(typeof(ConfigurationDbContext));
            services.RemoveAll(typeof(IConfigurationRepository));
            services.RemoveAll(typeof(IConfigurationMessagePublisher));
            services.RemoveAll(typeof(ConfigurationService));
            services.RemoveAll(typeof(RabbitMqConnectionFactory));
            services.RemoveAll(typeof(RabbitMqTopologyInitializer));
            services.RemoveAll(typeof(IConfigurationCache));
            services.RemoveAll(typeof(IDistributedCache));
            services.RemoveAll<IHostedService>();

            // In-memory DB
            services.AddDbContext<ConfigurationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));

            // In-memory distributed cache (no Redis needed for tests)
            services.AddDistributedMemoryCache();
            services.AddScoped<IConfigurationCache, RedisConfigurationCache>();

            services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
            services.AddScoped<ConfigurationService>();
            services.AddSingleton<IConfigurationMessagePublisher, NoOpConfigurationMessagePublisher>();

            // Replace JWT auth with a test scheme that always succeeds
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    /// <summary>Creates an HttpClient pre-authenticated as Admin.</summary>
    public HttpClient CreateAuthenticatedClient()
    {
        return CreateClient(); // TestAuthHandler auto-authenticates all requests
    }
}

internal sealed class NoOpConfigurationMessagePublisher : IConfigurationMessagePublisher
{
    public Task PublishAsync(Domain.Events.ConfigurationChangedEvent changedEvent, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
