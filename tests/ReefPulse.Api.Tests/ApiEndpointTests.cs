using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ReefPulse.Infrastructure;
using Testcontainers.PostgreSql;
using Xunit;

namespace ReefPulse.Api.Tests;

public sealed class PostgresApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16").Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<ReefDbContext>>();
            services.RemoveAll<ReefDbContext>();
            services.AddDbContext<ReefDbContext>(options =>
                options.UseNpgsql(_postgres.GetConnectionString()));
        });
    }

    public async Task InitializeAsync() => await _postgres.StartAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
    }
}

public sealed class ApiEndpointTests : IClassFixture<PostgresApiFactory>
{
    private readonly PostgresApiFactory _factory;

    public ApiEndpointTests(PostgresApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Liveness_probe_returns_200_and_healthy()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Readiness_probe_is_healthy_when_database_is_reachable()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Sites_endpoint_returns_seeded_reef_sites()
    {
        var client = _factory.CreateClient();

        var sites = await client.GetFromJsonAsync<List<SiteResponse>>("/sites");

        Assert.NotNull(sites);
        Assert.Equal(5, sites!.Count);
        Assert.Contains(sites, s => s.Name == "Negril");
    }

    private sealed record SiteResponse(Guid Id, string Name, string? Region, double Latitude, double Longitude);
}
