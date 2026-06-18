namespace DynamicConfig.Infrastructure.Auth;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "DynamicConfig";
    public string Audience { get; set; } = "DynamicConfigApi";
    public int ExpirationMinutes { get; set; } = 60;
}
