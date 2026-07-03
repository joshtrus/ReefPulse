var builder = WebApplication.CreateBuilder(args);

// The ASP.NET Core health-check subsystem. Right now it only reports "the process is up",
// but every future dependency (Postgres, Redis, Kafka) registers a check here so /health
// becomes a real readiness signal that Kubernetes and load balancers can poll.
builder.Services.AddHealthChecks();

var app = builder.Build();

app.MapGet("/", () => "ReefPulse API is running. See /health for status.");
app.MapHealthChecks("/health");

app.Run();

// Exposed so the integration test project can boot the real app in-memory via WebApplicationFactory<Program>.
public partial class Program { }
