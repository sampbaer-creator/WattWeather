using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace WattWeather.Tests;

public sealed class BackendSecurityTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public BackendSecurityTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Health_IsAvailableAndProtectedBySecurityHeaders()
    {
        using var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Contains("frame-ancestors 'none'", response.Headers.GetValues("Content-Security-Policy").Single());
    }

    [Fact]
    public async Task Weather_RejectsCoordinatesOutsideEarth()
    {
        using var response = await _client.GetAsync("/api/weather?latitude=91&longitude=0");
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Coordinates are outside valid ranges.", body?["error"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    public async Task LocationSearch_RejectsInvalidQueries(string query)
    {
        using var response = await _client.GetAsync($"/api/locations?query={Uri.EscapeDataString(query)}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
