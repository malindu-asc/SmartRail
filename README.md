# SmartRail

Public railway passenger and operations platform (individual assessment).

| Path | What |
|---|---|
| `src/Host` | Aspire AppHost, ServiceDefaults, API composition root |
| `src/BuildingBlocks` | SharedKernel, Application.Core, Api.Core, Infrastructure.Core |
| `src/Modules/<Name>` | Identity, Network, Journeys, Operations, Notifications (Clean Architecture per module) |
| `apps/mobile` | Flutter workspace: passenger and driver apps, shared packages |
| `apps/web` | React: ops/admin console and station kiosk (display + terminal modes) |
| `services/catering` | RailBite, external Node.js catering platform |
| `tools/Simulator` | Journey simulator, posts through the real ingestion endpoint |
| `tests` | Architecture tests and module tests |
| `docs` | Requirements, UX, architecture, QA, AI usage |

Backend rules live in `.claude/skills/smartrail-modular-monolith/SKILL.md`.

## Run

```bash
dotnet run --project src/Host/SmartRail.AppHost   # Postgres, API, simulator, catering, web apps
dotnet test                                       # architecture tests
```
