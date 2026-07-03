using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ReefPulse.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("ReefPulse")
    ?? throw new InvalidOperationException(
        "Missing connection string 'ReefPulse'. Set ConnectionStrings:ReefPulse in configuration.");

builder.Services.AddReefPersistence(connectionString);

// Two probes with distinct meanings, matching Kubernetes' liveness/readiness model:
//   /health/live  — is the process itself up? No dependencies. A failure means "restart me".
//   /health/ready — can we actually serve traffic? Checks Postgres. A failure means
//                   "keep me running but stop routing requests until my dependencies recover".
// Conflating the two is a classic outage cause: a blip in the DB restart-loops every pod.
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

// In Development, bring the schema up to date and seed reference data on boot so the app
// is immediately usable after `docker compose up`. In production you would instead run
// migrations as a deliberate, separate deploy step (e.g. an init job) rather than having
// every starting instance race to migrate.
if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<ReefDbContext>();
    await db.Database.MigrateAsync();
    await SeedData.EnsureSeedAsync(db);
}

app.Run();

// Exposed so the integration test project can boot the real app via WebApplicationFactory<Program>.
public partial class Program { }
