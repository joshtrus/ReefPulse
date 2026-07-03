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

- **Backend:** C# / .NET 8 / ASP.NET Core
- **Data:** PostgreSQL, Redis _(later milestones)_
- **Messaging:** Kafka _(later milestone)_
- **Observability:** OpenTelemetry, Prometheus, Grafana _(later milestones)_
- **Infra:** Docker Compose (local), GitHub Actions CI, Kubernetes _(later)_

## Repository layout

```
src/       application services (currently: ReefPulse.Api)
tests/     automated tests
Dockerfile, docker-compose.yml   local containerized run
.github/workflows/ci.yml         build + test on every push
```

## Running locally

Requires the .NET SDK (bundled with JetBrains Rider). From the repo root:

```bash
dotnet run --project src/ReefPulse.Api
```

Then open http://localhost:5000/health (Rider will print the exact port). It should return `Healthy`.

Run the tests:

```bash
dotnet test
```

Run it in a container (requires Docker):

```bash
docker compose up --build
# then: http://localhost:8080/health
```

## Roadmap

- [x] **M1 — Walking skeleton:** solution layout, health-checked API, integration test, Docker, CI
- [ ] **M2 — Persistence:** Postgres + EF Core, first domain model (reef sites / readings)
- [ ] **M3 — Ingestion service + Kafka:** decouple producers from consumers
- [ ] **M4 — Processing & anomaly detection**
- [ ] **M5 — Observability:** OpenTelemetry traces + Prometheus metrics + Grafana
- [ ] **M6 — Caching, resiliency (retries/circuit breakers), and hardening**
