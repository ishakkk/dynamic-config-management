using System.Net;
using System.Net.Http.Json;
using DynamicConfig.Domain.DTOs;
using DynamicConfig.Domain.Enums;
using FluentAssertions;

namespace DynamicConfig.IntegrationTests;

public class ConfigurationsApiTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ConfigurationsApiTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateAuthenticatedClient();
    }

    [Fact]
    public async Task CreateAndGetConfiguration_ShouldPersistEntry()
    {
        var request = new CreateConfigurationEntryRequest
        {
            Name = $"SiteName-{Guid.NewGuid():N}",
            Type = ConfigurationValueType.String,
            Value = "soty.io",
            IsActive = true,
            ApplicationName = "SERVICE-A"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/configurations", request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var listResponse = await _client.GetFromJsonAsync<List<ConfigurationEntryDto>>("/api/configurations");
        listResponse.Should().NotBeNull();
        listResponse!.Should().ContainSingle(x => x.Name == request.Name && x.ApplicationName == "SERVICE-A");
    }

    [Fact]
    public async Task CreateConfiguration_ShouldHaveVersionOne()
    {
        var request = new CreateConfigurationEntryRequest
        {
            Name = $"VersionTest-{Guid.NewGuid():N}",
            Type = ConfigurationValueType.String,
            Value = "initial",
            IsActive = true,
            ApplicationName = "SERVICE-A"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/configurations", request);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<ConfigurationEntryDto>();
        created.Should().NotBeNull();
        created!.Version.Should().Be(1);
    }

    [Fact]
    public async Task UpdateConfiguration_ShouldIncrementVersion()
    {
        var createRequest = new CreateConfigurationEntryRequest
        {
            Name = $"VersionIncr-{Guid.NewGuid():N}",
            Type = ConfigurationValueType.Int,
            Value = "50",
            IsActive = true,
            ApplicationName = "SERVICE-A"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/configurations", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ConfigurationEntryDto>();

        var updateRequest = new UpdateConfigurationEntryRequest
        {
            Name = createRequest.Name,
            Type = ConfigurationValueType.Int,
            Value = "100",
            IsActive = true,
            ApplicationName = "SERVICE-A"
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/configurations/{created!.Id}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await updateResponse.Content.ReadFromJsonAsync<ConfigurationEntryDto>();
        updated!.Value.Should().Be("100");
        updated.Version.Should().Be(created.Version + 1);
    }

    [Fact]
    public async Task UpdateConfiguration_ShouldModifyValue()
    {
        var createRequest = new CreateConfigurationEntryRequest
        {
            Name = $"MaxItemCount-{Guid.NewGuid():N}",
            Type = ConfigurationValueType.Int,
            Value = "50",
            IsActive = true,
            ApplicationName = "SERVICE-A"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/configurations", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ConfigurationEntryDto>();

        var updateRequest = new UpdateConfigurationEntryRequest
        {
            Name = createRequest.Name,
            Type = ConfigurationValueType.Int,
            Value = "100",
            IsActive = true,
            ApplicationName = "SERVICE-A"
        };

        var updateResponse = await _client.PutAsJsonAsync($"/api/configurations/{created!.Id}", updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await updateResponse.Content.ReadFromJsonAsync<ConfigurationEntryDto>();
        updated!.Value.Should().Be("100");
    }

    [Fact]
    public async Task GetByApplication_ShouldFilterRecords()
    {
        var serviceAName = $"SiteName-{Guid.NewGuid():N}";
        await _client.PostAsJsonAsync("/api/configurations", new CreateConfigurationEntryRequest
        {
            Name = serviceAName,
            Type = ConfigurationValueType.String,
            Value = "soty.io",
            IsActive = true,
            ApplicationName = "SERVICE-A"
        });

        await _client.PostAsJsonAsync("/api/configurations", new CreateConfigurationEntryRequest
        {
            Name = $"IsBasketEnabled-{Guid.NewGuid():N}",
            Type = ConfigurationValueType.Bool,
            Value = "1",
            IsActive = true,
            ApplicationName = "SERVICE-B"
        });

        var serviceAEntries = await _client.GetFromJsonAsync<List<ConfigurationEntryDto>>("/api/configurations/application/SERVICE-A");
        serviceAEntries.Should().NotBeNull();
        serviceAEntries!.Should().OnlyContain(x => x.ApplicationName == "SERVICE-A");
    }

    [Fact]
    public async Task DeleteConfiguration_ShouldReturnNoContent()
    {
        var createRequest = new CreateConfigurationEntryRequest
        {
            Name = $"ToDelete-{Guid.NewGuid():N}",
            Type = ConfigurationValueType.String,
            Value = "temp",
            IsActive = true,
            ApplicationName = "SERVICE-A"
        };

        var createResponse = await _client.PostAsJsonAsync("/api/configurations", createRequest);
        var created = await createResponse.Content.ReadFromJsonAsync<ConfigurationEntryDto>();

        var deleteResponse = await _client.DeleteAsync($"/api/configurations/{created!.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteConfiguration_WhenNotFound_ShouldReturnNotFound()
    {
        var response = await _client.DeleteAsync("/api/configurations/999999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task HealthCheck_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/health/live");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
