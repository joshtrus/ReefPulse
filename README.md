# ReefPulse

ReefPulse is a backend platform for monitoring the health of coral reefs around
**Jamaica**. It ingests real ocean data (sea-surface temperature and wave height) from an
external marine API on a schedule, stores it as time-series readings in PostgreSQL, and
serves it over a REST API.

Readings flow through an **event-driven pipeline**: a producer publishes each reading to a
Kafka topic; one consumer persists them, and a **second consumer detects bleaching-risk heat
stress** and raises alerts — decoupled so they scale and fail independently. The next
milestones add observability and a live dashboard. Everything is built one milestone at a
time — see the roadmap.

The focus of this project is the **backend**: distributed-systems design, an event
pipeline, resiliency, and first-class observability. The frontend exists only to visualize
the system.

## Architecture

Built today (data flows left to right):

```
Open-Meteo Marine API
   │  ingestion worker (timer)
   ▼
producer ─► Kafka topic `reef.readings` ─┬─► consumer "reefpulse-consumers" ─► PostgreSQL: readings
            3 partitions, keyed by site  │        (at-least-once, idempotent)
                                         └─► consumer "reefpulse-detectors" ─► PostgreSQL: alerts
                                                  (bleaching-risk detection)
PostgreSQL ─► REST API   (readings + alerts)
```

Two independent consumer groups read the same topic — one persists, one detects — so neither
blocks nor duplicates the other. Planned in later milestones: a Redis hot cache and
Prometheus/Grafana observability. Infrastructure is added the moment a feature needs it.

## Tech stack

- **Backend:** C# / .NET 9 / ASP.NET Core
- **Data:** PostgreSQL (EF Core), Redis _(later milestone)_
- **Messaging:** Kafka (Confluent.Kafka client, KRaft broker)
- **Observability:** OpenTelemetry, Prometheus, Grafana _(later milestones)_
- **Infra:** Docker Compose (local), GitHub Actions CI, Kubernetes _(later)_

## Repository layout

The solution follows a layered (Clean Architecture) split so dependencies point inward —
the domain knows nothing about the database, and the API knows nothing about which
database it is:

```
src/
  ReefPulse.Domain           entities only (ReefSite, Reading, Alert) — zero dependencies
  ReefPulse.Infrastructure   EF Core + Npgsql, migrations, Open-Meteo ingestion, Kafka producer/consumers, bleaching detection
  ReefPulse.Api              HTTP surface; references Infrastructure
tests/
  ReefPulse.Api.Tests        integration + pipeline tests (real Postgres & Kafka via Testcontainers)
Dockerfile, docker-compose.yml   local containerized run (API + Postgres + Kafka)
.github/workflows/ci.yml         build + test on every push
```

Dependency rule: `Api → Infrastructure → Domain` (Domain depends on nothing).

## API surface

| Endpoint        | Purpose                                                              |
|-----------------|---------------------------------------------------------------------|
| `/health/live`  | Liveness probe — process is up (no dependencies).                   |
| `/health/ready` | Readiness probe — can serve traffic; includes a PostgreSQL check.   |
| `/sites`        | Lists monitored reef sites.                                         |
| `POST /sites/{id}/readings` | Records a reading for a site (`409` if an identical reading already exists). |
| `GET /sites/{id}/readings`  | Lists a site's readings, newest first; optional `?metric=` and `?limit=` (default 50, max 500). |
| `GET /alerts`               | Lists bleaching alerts; `?status=Active` (default) or `Resolved`. |
| `GET /sites/{id}/alerts`    | Lists a single site's alerts, newest first. |

## Data ingestion & the event pipeline

A background worker (`MarineIngestionWorker`) polls the
[Open-Meteo Marine API](https://open-meteo.com/) on a timer and, for every reef site,
**publishes** sea-surface-temperature and wave-height readings as events to the Kafka topic
`reef.readings` — keyed by site id so a site's readings stay ordered within a partition. It
creates a fresh DI scope per tick, isolates failures per site, and applies an HTTP timeout so
a slow upstream can't stall the loop.

A consumer (`ReadingConsumerWorker`, group `reefpulse-consumers`) reads those events and
persists them to PostgreSQL with **at-least-once** delivery — it commits its offset only after
a successful write. A unique index on `(site, metric, observed-at, source)` makes the consumer
**idempotent**, so a redelivered event is a no-op rather than a duplicate row.

A **second consumer group** (`reefpulse-detectors`, `BleachingDetectionWorker`) reads the same
topic independently and flags **bleaching risk**: when a site's water temperature reaches the
configured threshold it opens an `Alert`, and resolves it when the reef cools. Because each
group tracks its own offsets, detection and persistence never block or duplicate each other.
The detection logic is a pure function (`BleachingDetector`) and is naturally idempotent — it
decides from the site's current alert state, so a redelivered event can't double-open.

Configuration:

| Section | Setting | Default | Notes |
|---------|---------|---------|-------|
| `Ingestion` | `Enabled` | `false` | Off by default so tests/CI never hit the network; enabled in `docker-compose.yml`. |
| `Ingestion` | `IntervalMinutes` | `10` | Poll frequency; first poll runs immediately on startup. |
| `Kafka` | `BootstrapServers` | `localhost:9092` | Broker address (`kafka:9092` in Compose). |
| `Kafka` | `ReadingsTopic` | `reef.readings` | Auto-created on startup (3 partitions). |
| `Kafka` | `ConsumerGroup` | `reefpulse-consumers` | Persistence consumer group id. |
| `Bleaching` | `TemperatureThresholdCelsius` | `30.5` | Water temp at/above which a bleaching-risk alert opens. |

## Running locally

The simplest path is Docker, which starts the API and PostgreSQL together. The API applies
EF Core migrations and seeds a few reef sites on startup, so it is usable immediately:

```bash
cp .env.example .env      # one-time: local-dev credentials (this .env is git-ignored)
docker compose up --build
# then:
#   http://localhost:8080/health/live           -> Healthy
#   http://localhost:8080/health/ready          -> Healthy (once Postgres is up)
#   http://localhost:8080/sites                 -> seeded Jamaican reef sites
#   http://localhost:8080/sites/{id}/readings   -> live readings (ingested from Open-Meteo)
#   http://localhost:8080/alerts                -> active bleaching-risk alerts
#
# Ingestion is enabled in Compose, so each reef gets real readings within seconds.
```

To run the API on the host instead, start just the database in a container and point the
API at it (the connection string in `appsettings.Development.json` targets `localhost`):

```bash
docker compose up -d postgres
dotnet run --project src/ReefPulse.Api
```

Run the tests (they spin up throwaway Postgres and Kafka via Testcontainers, so Docker
must be running):

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
- [x] **M3 — Kafka:** event-driven pipeline (producer → topic → consumer), per-site keyed partitioning, at-least-once + idempotent consumer, end-to-end Testcontainers test
- [x] **M4 — Anomaly detection:** second Kafka consumer group flags bleaching-risk heat stress and opens/resolves alerts; alerts API; unit + end-to-end tests
- [ ] **M5 — Observability:** OpenTelemetry traces + Prometheus metrics + Grafana
- [ ] **M6 — Caching, resiliency (retries/circuit breakers), and hardening**
