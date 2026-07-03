# ReefPulse

Real-time platform for monitoring coral reef health. ReefPulse ingests multiple
environmental data streams (ocean temperature, tides, wave data, Coral Reef Watch,
synthetic sensors), processes them through an event-driven pipeline, detects anomalies,
and surfaces live reef health on a dashboard.

The focus of this project is the **backend**: distributed-systems design, an event
pipeline, caching, resiliency, and first-class observability. The frontend exists only
to visualize the system.

## Architecture (target)

```
data sources ──> ingestion service ──> Kafka ──> processing / anomaly detection
                                                        │
                                          ┌─────────────┼──────────────┐
                                       Postgres       Redis        metrics
                                     (source of      (hot cache)   (Prometheus)
                                       truth)                          │
                                                                    Grafana
```

Not everything above exists yet. It is built one milestone at a time, and infrastructure
is only added the moment a feature needs it.

## Tech stack

- **Backend:** C# / .NET 9 / ASP.NET Core
- **Data:** PostgreSQL (EF Core), Redis _(later milestone)_
- **Messaging:** Kafka _(later milestone)_
- **Observability:** OpenTelemetry, Prometheus, Grafana _(later milestones)_
- **Infra:** Docker Compose (local), GitHub Actions CI, Kubernetes _(later)_

## Repository layout

The solution follows a layered (Clean Architecture) split so dependencies point inward —
the domain knows nothing about the database, and the API knows nothing about which
database it is:

```
src/
  ReefPulse.Domain           entities only (ReefSite, Reading) — zero dependencies
  ReefPulse.Infrastructure   EF Core DbContext, mappings, migrations, Npgsql
  ReefPulse.Api              HTTP surface; references Infrastructure
tests/
  ReefPulse.Api.Tests        integration tests (real Postgres via Testcontainers)
Dockerfile, docker-compose.yml   local containerized run (API + Postgres)
.github/workflows/ci.yml         build + test on every push
```

Dependency rule: `Api → Infrastructure → Domain` (Domain depends on nothing).

## API surface

| Endpoint        | Purpose                                                              |
|-----------------|---------------------------------------------------------------------|
| `/health/live`  | Liveness probe — process is up (no dependencies).                   |
| `/health/ready` | Readiness probe — can serve traffic; includes a PostgreSQL check.   |
| `/sites`        | Lists monitored reef sites.                                         |

## Running locally

The simplest path is Docker, which starts the API and PostgreSQL together. The API applies
EF Core migrations and seeds a few reef sites on startup, so it is usable immediately:

```bash
docker compose up --build
# then:
#   http://localhost:8080/health/live    -> Healthy
#   http://localhost:8080/health/ready   -> Healthy (once Postgres is up)
#   http://localhost:8080/sites          -> JSON list of seeded reef sites
```

To run the API on the host instead, start just the database in a container and point the
API at it (the connection string in `appsettings.Development.json` targets `localhost`):

```bash
docker compose up -d postgres
dotnet run --project src/ReefPulse.Api
```

Run the tests (integration tests spin up a throwaway Postgres via Testcontainers, so
Docker must be running):

```bash
dotnet test
```

### Database migrations

EF Core migrations live in `src/ReefPulse.Infrastructure/Migrations`. To add one after
changing the model:

```bash
dotnet tool restore   # once, installs the pinned dotnet-ef
dotnet ef migrations add <Name> -p src/ReefPulse.Infrastructure -s src/ReefPulse.Api
```

## Roadmap

- [x] **M1 — Walking skeleton:** solution layout, health-checked API, integration test, Docker, CI
- [x] **M2 — Persistence:** Postgres + EF Core, layered domain (reef sites / readings), migrations, liveness/readiness probes, Testcontainers integration tests
- [ ] **M3 — Ingestion service + Kafka:** decouple producers from consumers
- [ ] **M4 — Processing & anomaly detection**
- [ ] **M5 — Observability:** OpenTelemetry traces + Prometheus metrics + Grafana
- [ ] **M6 — Caching, resiliency (retries/circuit breakers), and hardening**
