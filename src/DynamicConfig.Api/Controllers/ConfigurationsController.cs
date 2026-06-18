using DynamicConfig.Domain.DTOs;
using DynamicConfig.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DynamicConfig.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConfigurationsController : ControllerBase
{
    private readonly ConfigurationService _configurationService;
    private readonly ILogger<ConfigurationsController> _logger;

    public ConfigurationsController(ConfigurationService configurationService, ILogger<ConfigurationsController> logger)
    {
        _configurationService = configurationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ConfigurationEntryDto>>> GetAll(CancellationToken cancellationToken)
    {
        var items = await _configurationService.GetAllAsync(cancellationToken);
        return Ok(items.Select(MapToDto));
    }

    [HttpGet("application/{applicationName}")]
    public async Task<ActionResult<IEnumerable<ConfigurationEntryDto>>> GetByApplication(
        string applicationName,
        CancellationToken cancellationToken)
    {
        var items = await _configurationService.GetByApplicationAsync(applicationName, cancellationToken);
        return Ok(items.Select(MapToDto));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ConfigurationEntryDto>> Create(
        [FromBody] CreateConfigurationEntryRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _configurationService.CreateAsync(request, cancellationToken);
        _logger.LogInformation("Created configuration {Name} for {ApplicationName}", created.Name, created.ApplicationName);
        return CreatedAtAction(nameof(GetAll), new { id = created.Id }, MapToDto(created));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ConfigurationEntryDto>> Update(
        int id,
        [FromBody] UpdateConfigurationEntryRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _configurationService.UpdateAsync(id, request, cancellationToken);
        if (updated is null)
        {
            return NotFound();
        }

        _logger.LogInformation("Updated configuration {Id} (Version={Version})", id, updated.Version);
        return Ok(MapToDto(updated));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await _configurationService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }

    private static ConfigurationEntryDto MapToDto(Domain.Entities.ConfigurationEntry entry) =>
        new()
        {
            Id = entry.Id,
            Name = entry.Name,
            Type = entry.Type,
            Value = entry.Value,
            IsActive = entry.IsActive,
            ApplicationName = entry.ApplicationName,
            UpdatedAt = entry.UpdatedAt,
            Version = entry.Version
        };
}
