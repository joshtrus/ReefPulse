using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ReefPulse.Api.Contracts;
using ReefPulse.Domain;
using ReefPulse.Infrastructure;
using ReefPulse.Infrastructure.Ingestion;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("ReefPulse")
    ?? throw new InvalidOperationException(
        "Missing connection string 'ReefPulse'. Set ConnectionStrings:ReefPulse in configuration.");

builder.Services.AddReefPersistence(connectionString);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddReefIngestion(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddDbContextCheck<ReefDbContext>("database", tags: ["ready"]);

var app = builder.Build();

app.MapGet("/", () => "ReefPulse API is running. See /health/live and /health/ready.");

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapGet("/sites", async (ReefDbContext db, CancellationToken ct) =>
    await db.ReefSites
        .OrderBy(s => s.Name)
        .Select(s => new { s.Id, s.Name, s.Region, s.Latitude, s.Longitude })
        .ToListAsync(ct));

app.MapPost("/sites/{siteId:guid}/readings", async (
    Guid siteId, CreateReadingRequest request, ReefDbContext db, CancellationToken ct) =>
{
    if (!await db.ReefSites.AnyAsync(s => s.Id == siteId, ct))
        return Results.NotFound($"No reef site with id {siteId}.");

    var reading = new Reading
    {
        ReefSiteId = siteId,
        Metric = request.Metric,
        Value = request.Value,
        ObservedAt = request.ObservedAt,
        Source = request.Source
    };

    db.Readings.Add(reading);
    await db.SaveChangesAsync(ct);

    return Results.Created($"/sites/{siteId}/readings/{reading.Id}", new { reading.Id });
});

app.MapGet("/sites/{siteId:guid}/readings", async (
    Guid siteId, MetricType? metric, int? limit, ReefDbContext db, CancellationToken ct) =>
{
    if (!await db.ReefSites.AnyAsync(s => s.Id == siteId, ct))
        return Results.NotFound($"No reef site with id {siteId}.");

    var take = limit ?? 50;
    if (take < 1) take = 1;
    else if (take > 500) take = 500;

    var query = db.Readings.Where(r => r.ReefSiteId == siteId);
    if (metric is not null)
        query = query.Where(r => r.Metric == metric.Value);

    var readings = await query
        .OrderByDescending(r => r.ObservedAt)
        .Take(take)
        .Select(r => new { r.Id, r.Metric, r.Value, r.ObservedAt, r.RecordedAt, r.Source })
        .ToListAsync(ct);

    return Results.Ok(readings);
});


if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<ReefDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.EnsureSeedAsync(db);
}

app.Run();

public partial class Program { }
