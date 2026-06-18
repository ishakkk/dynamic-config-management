using DynamicConfig.Infrastructure.Auth;
using Microsoft.AspNetCore.Mvc;

namespace DynamicConfig.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly TokenService _tokenService;

    // Hard-coded demo credentials – replace with a proper user store in production.
    private static readonly Dictionary<string, (string Password, string[] Roles)> Users = new(StringComparer.OrdinalIgnoreCase)
    {
        ["admin"] = ("admin123", ["Admin"]),
        ["reader"] = ("reader123", ["Reader"])
    };

    public AuthController(TokenService tokenService)
    {
        _tokenService = tokenService;
    }

    /// <summary>
    /// Returns a JWT bearer token. Use it in the Authorization header: Bearer {token}
    /// </summary>
    [HttpPost("token")]
    public IActionResult Token([FromBody] LoginRequest request)
    {
        if (!Users.TryGetValue(request.ClientId, out var user) ||
            user.Password != request.Secret)
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var token = _tokenService.GenerateToken(request.ClientId, user.Roles);
        return Ok(new { token });
    }
}

public sealed class LoginRequest
{
    public string ClientId { get; set; } = string.Empty;
    public string Secret { get; set; } = string.Empty;
}
