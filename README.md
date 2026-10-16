# SmartRail

SmartRail is a public railway **passenger and operations platform**. It shows passengers what a train is doing right now, lets drivers report progress, and gives controllers the tools to change the service. Every time on every screen says how fresh it is, so people are never misled by old data.

## Features

| For | What it provides |
|---|---|
| **Passengers** | Live departures and a journey timeline, each time labelled Live, Stale or Unknown; follow a journey to get updates |
| **Drivers** | Duty view, record departed and arrived at each stop, works offline and syncs when back online |
| **Network controllers** | Live network board; cancel, terminate or change platform with a reason, all audited |
| **System administrators** | Manage users, roles and station devices, kept separate from train operations |
| **Stations** | Departure displays and an interactive touch terminal |

Design principles:

- **Honest information:** data freshness is always visible. Stale after 60 seconds without data, unknown after 5 minutes.
- **Scheduled time never changes** when a delay is recorded.
- **Every operator change is audited** (who, what, why, when).
- **Separation of duties** between operating trains and administering users.
- **Information only:** nothing in the system is safety-critical.

## Architecture

```
 Flutter apps            React apps              Simulator
 passenger, driver       console, kiosk          (test train events)
        \                    |                      /
         \                   |                     /
          ------->   .NET 10 API (one deployable)  <-------
                     5 modules - one PostgreSQL DB
                            |                |
                         Keycloak       Catering (Node.js, HTTP only)
```

- **Modular monolith** with Clean Architecture inside each module and one database schema per module.
- **CQRS with MediatR**; `Result` and typed `Error` values instead of exceptions; FluentValidation in the pipeline.
- **In-process events through a transactional outbox**, so a change and its events are saved together. No message broker.
- **Keycloak** for identity: users, roles and tokens. The API validates tokens.
- **OpenTelemetry** in every service, with Grafana, Tempo, Loki and Prometheus.

### Modules

| Module | Responsibility |
|---|---|
| `Identity` | Role-to-permission mapping, audit log, device tokens |
| `Network` | Stations, routes, timetables, journeys, driver and unit assignments |
| `Journeys` | Live journey state, confidence, stop events, departure board |
| `Operations` | Cancel, terminate, platform change, disruptions, driver messages |
| `Notifications` | Realtime push to clients |

Each module is split into `Domain`, `Application`, `Infrastructure`, `Api` and `Contracts`. Modules talk to each other only through `Contracts`, and architecture tests enforce it.

## Repository structure

```
SmartRail/
├── src/
│   ├── Host/
│   │   ├── SmartRail.AppHost            Aspire orchestration (dev)
│   │   ├── SmartRail.ServiceDefaults    OpenTelemetry, health checks
│   │   └── SmartRail.Api                API composition root
│   ├── BuildingBlocks/                  SharedKernel, Application.Core, Api.Core, Infrastructure.Core
│   └── Modules/
│       └── <Module>/                    Domain, Application, Infrastructure, Api, Contracts
├── apps/
│   ├── mobile/                          Flutter workspace
│   │   ├── apps/                        passenger, driver
│   │   └── packages/                    core, design_system, api_client
│   └── web/                             React workspace
│       ├── console/                     controller and admin
│       └── kiosk/                       station display and terminal
├── services/catering/                   External catering service (Node.js)
├── tools/Simulator/                     Train event simulator
├── tests/                               Architecture and module tests
├── infra/
│   ├── keycloak/                        Realm import (roles, clients, dev users)
│   └── observability/                   Collector, Tempo, Loki, Prometheus, Grafana
├── .github/workflows/                   CI, image publishing, Azure deployment
├── docker-compose.yml                   Full stack in containers
└── SmartRail.slnx
```

## Prerequisites

| Tool | Version |
|---|---|
| .NET SDK | 10.0 |
| Docker Desktop | recent, running |
| Node.js | 24 |
| Flutter | 3.44 (Dart 3.12) |

## Running the project

### Development, with Aspire

```bash
dotnet run --project src/Host/SmartRail.AppHost
```

Open the dashboard link printed in the terminal (usually `https://localhost:17063`). Aspire starts PostgreSQL, Keycloak, the API, the simulator, the catering service and both web apps, and wires their connections. Wait until every resource is green.

To include the Grafana stack:

```bash
dotnet run --project src/Host/SmartRail.AppHost -- --Observability:Enabled=true
```

Grafana is then at http://localhost:3002 (`admin` / `admin`), dashboard **SmartRail platform**.

### Full stack, with Docker Compose

```bash
cp .env.example .env        # set the passwords
docker compose up -d --build
docker compose ps
```

| URL | Service |
|---|---|
| http://localhost:5080 | API (`/scalar` for API docs in Development) |
| http://localhost:8080 | Keycloak |
| http://localhost:3000 | Console |
| http://localhost:3001 | Kiosk |
| http://localhost:4000/health | Catering |
| http://localhost:3002 | Grafana |
| http://localhost:9090 | Prometheus |

Stop with `docker compose down` (add `-v` to remove data volumes). Do not run Aspire and Compose together, because they use the same ports.

### Individual apps

```bash
# web (console or kiosk)
cd apps/web && npm install && npm run dev --workspace=console

# catering service
cd services/catering && npm install && npm run dev

# mobile
cd apps/mobile && flutter pub get
cd apps/passenger && flutter run      # or apps/driver
```

## Testing

```bash
dotnet test                                                  # unit, architecture and API tests (API tests need Docker)
npm run build --workspace=console --prefix apps/web          # web build
npm run typecheck --prefix services/catering                 # catering typecheck
cd apps/mobile/apps/passenger && flutter test                # mobile tests
```

CI runs all of the above plus a Docker build of every image on each push and pull request.

## Signing in (development)

Keycloak seeds one user per role: `passenger`, `driver`, `controller` and `admin`. Their development passwords are in `infra/keycloak/smartrail-realm.json`. They are for local use only.

### Getting a token (development)

```bash
scripts/get-token.sh controller
curl -H "Authorization: Bearer $(scripts/get-token.sh controller)" http://localhost:5080/api/identity/whoami
```

Every endpoint requires a signed-in user unless it is explicitly marked anonymous; a test fails the build if an
unexpected endpoint becomes public. Roles map to the `Driver`, `Controller` and `Admin` policies.

## Network data (public API)

Stations and routes are public, read-only and need no token. Names follow `?lang=` or `Accept-Language`
(`en` default, `si`).

```bash
curl "http://localhost:5080/api/network/stations?q=oak&lang=si"
curl  http://localhost:5080/api/network/stations/CEN
curl  http://localhost:5080/api/network/routes
curl  http://localhost:5080/api/network/routes/HILL
```

The demo network is inserted at startup when `Network__Seed__ReferenceData=true` (set by the Aspire AppHost and
Docker Compose). It only adds missing rows and never changes existing ones.

## Database migrations

Each module has its own DbContext and schema. From the repository root:

```bash
dotnet ef migrations add <Name> \
  --project src/Modules/<Module>/SmartRail.Modules.<Module>.Infrastructure \
  --context <Module>DbContext \
  --output-dir Persistence/Migrations
```

Migrations are applied automatically on startup in Development only.

## Observability

Services export OpenTelemetry to a collector, which sends traces to Tempo, logs to Loki and metrics to Prometheus. Grafana is pre-provisioned with these data sources, a **SmartRail platform** dashboard and alert rules for API errors, latency, outbox failures and missing telemetry.

## CI/CD

| Workflow | Purpose |
|---|---|
| `ci.yml` | Backend build and tests, web build, catering typecheck, Flutter analyze and test, Docker image builds |
| `cd.yml` | Publishes images to GitHub Container Registry from `main` |
| `deploy-azure.yml` | Manual `azd` deployment to Azure Container Apps |

## Security checks

| Check | Where |
|---|---|
| Secret scanning (Gitleaks) | `security.yml` |
| Static analysis (CodeQL, C# and TypeScript) | `security.yml` |
| Vulnerable NuGet and npm packages | `security.yml` |
| Repository and Dockerfile scan (Trivy) | `security.yml` |
| Image scan (Trivy) | `ci.yml`, per image |
| Dependency updates | `dependabot.yml`, weekly |

## Troubleshooting

| Symptom | Fix |
|---|---|
| `dotnet run` prints "Building..." and returns | Reset Aspire's socket folder: `Remove-Item -Recurse -Force "$env:USERPROFILE\.aspire\cli\bch"`, then run again. Logs are in `%USERPROFILE%\.aspire\logs` |
| Build says a file is locked by `SmartRail.AppHost` | The AppHost is still running. Press Ctrl+C in its terminal |
| Cannot connect to the Docker daemon | Start Docker Desktop and wait until it is running |
| Port already in use | Stop the other stack (Aspire or Compose) |
| Keycloak shows Unhealthy | It is usually still starting. Wait a minute, then check its log |
| `flutter pub get` fails on the workspace | Run it from `apps/mobile`, not from a package folder |
| Grafana panels are empty | Send a few requests to the API; panels fill once there is traffic |
