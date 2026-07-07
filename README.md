# ReefPulse

ReefPulse is a backend platform for monitoring the health of coral reefs around
**Jamaica**. It ingests real ocean data (sea-surface temperature and wave height) from an
external marine API on a schedule, stores it as time-series readings in PostgreSQL, and
serves it over a REST API.

The longer-term goal is an event-driven pipeline that detects anomalies — such as the heat
stress that drives coral bleaching — and surfaces live reef health on a dashboard. That
direction is shown under _Architecture (target)_ below and is built one milestone at a time.

The focus of this project is the **backend**: distributed-systems design, an event
pipeline, resiliency, and first-class observability. The frontend exists only to visualize
the system.

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
  ReefPulse.Infrastructure   EF Core DbContext, mappings, migrations, Npgsql, Open-Meteo ingestion
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
| `POST /sites/{id}/readings` | Records a reading (metric, value, observed-at, source) for a site. |
| `GET /sites/{id}/readings`  | Lists a site's readings, newest first; optional `?metric=` and `?limit=` (default 50, max 500). |

## Data ingestion

A background worker (`MarineIngestionWorker`) polls the
[Open-Meteo Marine API](https://open-meteo.com/) on a timer and records **sea-surface
temperature** and **wave height** for every reef site. It creates a fresh dependency-injection
scope per tick (so the scoped `DbContext` isn't held for the worker's lifetime), isolates
failures per site, and applies an HTTP timeout so a slow upstream can't stall the loop.

Configuration lives under the `Ingestion` section (env vars `Ingestion__Enabled`,
`Ingestion__IntervalMinutes`):

| Setting | Default | Notes |
|---------|---------|-------|
| `Enabled` | `false` | Off by default so tests and CI never hit the network. Enabled in `docker-compose.yml`. |
| `IntervalMinutes` | `10` | Poll frequency. The first poll runs immediately on startup. |

## Running locally

The simplest path is Docker, which starts the API and PostgreSQL together. The API applies
EF Core migrations and seeds a few reef sites on startup, so it is usable immediately:

```bash
docker compose up --build
# then:
#   http://localhost:8080/health/live           -> Healthy
#   http://localhost:8080/health/ready          -> Healthy (once Postgres is up)
#   http://localhost:8080/sites                 -> seeded Jamaican reef sites
#   http://localhost:8080/sites/{id}/readings   -> live readings (ingested from Open-Meteo)
#
# Ingestion is enabled in Compose, so each reef gets real readings within seconds.
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
- [x] **M2.5 — Ingestion (synchronous):** readings API (POST/GET), real Open-Meteo background ingestion for Jamaican reefs, config-gated
- [ ] **M3 — Kafka:** split ingestion into producer → topic → consumer to decouple fetching from persistence
- [ ] **M4 — Processing & anomaly detection**
- [ ] **M5 — Observability:** OpenTelemetry traces + Prometheus metrics + Grafana
- [ ] **M6 — Caching, resiliency (retries/circuit breakers), and hardening**
