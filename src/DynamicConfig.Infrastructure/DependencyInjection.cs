using DynamicConfig.Domain.Interfaces;
using DynamicConfig.Infrastructure.Auth;
using DynamicConfig.Infrastructure.Caching;
using DynamicConfig.Infrastructure.Messaging;
using DynamicConfig.Infrastructure.Persistence;
using DynamicConfig.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace DynamicConfig.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDynamicConfigInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // =========================
        // RabbitMQ Options
        // =========================
        _ = services.Configure<RabbitMqOptions>(
            configuration.GetSection(RabbitMqOptions.SectionName));

        // =========================
        // EF Core
        // =========================
        _ = services.AddDbContext<ConfigurationDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection")));

        // =========================
        // Redis / Cache
        // =========================
        var redisConnectionString = configuration.GetConnectionString("Redis");

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            _ = services.AddStackExchangeRedisCache(opts =>
            {
                opts.Configuration = redisConnectionString;
                opts.InstanceName = "DynamicConfig:";
            });

            _ = services.AddScoped<IConfigurationCache, RedisConfigurationCache>();
        }
        else
        {
            _ = services.AddDistributedMemoryCache();
            _ = services.AddScoped<IConfigurationCache, RedisConfigurationCache>();
        }

        // =========================
        // JWT
        // =========================
        _ = services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SectionName));

        _ = services.AddSingleton<TokenService>();

        var jwtSection = configuration.GetSection(JwtOptions.SectionName);

        var secret = jwtSection["Secret"] ?? "DefaultDevSecretKeyAtLeast32CharsLong!!";
        var issuer = jwtSection["Issuer"] ?? "DynamicConfig";
        var audience = jwtSection["Audience"] ?? "DynamicConfigApi";

        _ = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey =
                        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
                };
            });

        _ = services.AddAuthorization();

        // =========================
        // Health Checks
        // =========================
        var healthChecks = services.AddHealthChecks();

        _ = healthChecks.AddSqlServer(
            configuration.GetConnectionString("DefaultConnection")!,
            name: "sqlserver",
            tags: new[] { "db", "sql" });

        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            _ = healthChecks.AddRedis(
                redisConnectionString,
                name: "redis",
                tags: new[] { "cache", "redis" });
        }

        // RabbitMQ HealthCheck (EN STABİL HAL)
        var rabbitOptions = configuration
            .GetSection(RabbitMqOptions.SectionName)
            .Get<RabbitMqOptions>();

        if (rabbitOptions != null)
        {
            var rabbitConnection =
                $"amqp://{rabbitOptions.UserName}:{rabbitOptions.Password}" +
                $"@{rabbitOptions.HostName}:{rabbitOptions.Port}";

            _ = healthChecks.AddRabbitMQ(
                rabbitConnection,
                name: "rabbitmq",
                tags: new[] { "mq", "rabbitmq" });
        }

        // =========================
        // Messaging
        // =========================
        _ = services.AddSingleton<RabbitMqConnectionFactory>();
        _ = services.AddSingleton<RabbitMqTopologyInitializer>();
        _ = services.AddSingleton<IConfigurationMessagePublisher, RabbitMqConfigurationMessagePublisher>();

        _ = services.AddHostedService<ConfigurationChangeConsumerService>();

        // =========================
        // Repository + Service
        // =========================
        _ = services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
        _ = services.AddScoped<ConfigurationService>();

        return services;
    }
}