using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ReefPulse.Api.Tests;

// Boots the whole API in-memory (no network port) and hits it like a real client.
// This is an integration test, not a unit test: it proves the wiring works end to end.
public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Health_endpoint_returns_200_and_healthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
