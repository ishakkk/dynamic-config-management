using DynamicConfig.Library;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new Exception("DB connection missing");
}
var refreshInterval = builder.Configuration.GetValue("DynamicConfig:RefreshTimerIntervalInMs", 5000);
var configurationReader = new ConfigurationReader("SERVICE-A", connectionString, refreshInterval);

builder.Services.AddSingleton(configurationReader);

var app = builder.Build();

app.MapGet("/", (ConfigurationReader reader) => Results.Ok(new
{
    Application = "SERVICE-A",
    SiteName = reader.GetValue<string>("SiteName"),
    MaxItemCountAvailable = reader.TryGetValue<int>("MaxItemCount", out _)
}));

app.MapGet("/config/{key}", (string key, ConfigurationReader reader) =>
{
    if (!reader.TryGetValue<string>(key, out var value))
    {
        return Results.NotFound(new { message = $"Key '{key}' not found for SERVICE-A." });
    }

    return Results.Ok(new { key, value });
});

app.Run();
