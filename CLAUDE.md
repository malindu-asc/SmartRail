# SmartRail: guide for AI agents

SmartRail is a national railway platform: passenger and driver mobile apps, a controller/scheduler/admin console, station terminals and a separate catering service ("RailBite"). Read this file first, then load the skill that matches the work (see "Skills").

## Stack and layout

| Part | Tech | Where |
|---|---|---|
| API | .NET 10 modular monolith, PostgreSQL (one schema per module), MediatR | `src/` |
| Login | Keycloak (realm in `infra/keycloak/smartrail-realm.json`) | container |
| Console | React 19, Vite, TypeScript, Tailwind v4, TanStack Query | `apps/web/console` |
| Station terminals | React, Vite, plain CSS, polling | `apps/web/kiosk` |
| Passenger and driver apps | Flutter, Riverpod, go_router, fpdart | `apps/mobile` |
| Catering | Node.js, Express | `services/catering` |
| Specs and docs | Markdown | `specs/`, `docs/` |
| Tests | xUnit, Vitest, node:test, flutter test, Playwright | `tests/`, `apps/*/`, `apps/web/e2e` |

Backend modules: `Identity`, `Network`, `Journeys`, `Operations`, `Ticketing`, `Notifications`. Each has `Api`, `Application`, `Domain`, `Infrastructure`, `Contracts`. Shared plumbing is in `src/BuildingBlocks/`.

## Run it

```bash
docker compose up -d --build        # API 5080, Keycloak 8080, console 3000, kiosk 3001, catering 4000
cd apps/web/kiosk && npm run dev    # terminals on 5173 (use this, not the kiosk container)
```

Dev logins (`admin`, `controller`, `scheduler`, `driver`, `passenger` and a few more, all `@gmail.com`) are in the realm file. They are development accounts only.

## Check your work (the same commands CI runs)

```bash
dotnet build SmartRail.slnx -c Release -warnaserror
dotnet test SmartRail.slnx -c Release
cd apps/web && npm ci && npm run build --workspace=console && npm run build --workspace=kiosk
cd apps/web/console && npm test            # kiosk: npm test in apps/web/kiosk
cd services/catering && npm run typecheck
cd apps/mobile && flutter analyze apps/passenger apps/driver packages/core packages/design_system packages/api_client
cd apps/mobile/apps/driver && flutter test # and apps/passenger
```

`/verify` runs the relevant ones for the files you changed. Report only checks you actually ran. Say plainly what you did not run and why.

## Rules that matter

**Backend**
- Modules talk to each other only through `Contracts`. Architecture tests enforce it.
- A command handler loads the aggregate, changes it through the Domain, records the audit entry and calls `SaveChangesAsync` once. The change and its audit entry save together.
- Every operator action needs a permission (`src/BuildingBlocks/SmartRail.Api.Core/Security/Permissions.cs`) and an audit entry.
- A schema change needs a migration (`dotnet ef migrations add <Name> --project <Module>.Infrastructure --context <Module>DbContext`).
- Public read endpoints (boards, notices) are anonymous and read-only. Never put secrets or personal data in logs. Strip line breaks from request values before logging.

**Web**
- Every API call goes through `apps/web/console/src/shared/api/httpClient.ts`. Features wrap it in a hook under `features/<feature>/hooks/`. Screens never call `fetch`.
- The terminal shows live data only. It must never show a confident wrong answer: no live update means "Scheduled", with the age of the data in the footer.

**Mobile**
- State is Riverpod. Results are `Either<Failure, T>`. Driver app strings live in `app_en.arb` and `app_si.arb` and must stay in step (placeholder parameters are alphabetical).

**Demo behaviour that is deliberate (do not "fix" without asking)**
- Notices publish at once with no second approver, and end by themselves at their expected end (the public list hides them). Timetable versions can be approved by their author.
- Nothing is pre-assigned in the seed. The scheduler presses Generate plan and Approve plan (Planning, Week). A driver sees a day's duty only after approval (`Journeys:Planning:ApprovalRequired`).
- The compose file relaxes the live-data thresholds to 900 and 1800 seconds because a driver taps by hand.
- Terminals are anonymous; the device register records and monitors them but does not enforce keys yet.

## Workflow

- Non-trivial work follows `specs/` (see `specs/README.md` and the `spec-driven-development` skill). Build only what the approved spec says, and write a test per acceptance criterion.
- Branch `feature/<name>`, pull requests into `dev`, `dev` into `main`. Use the pull request template, including "Checks run" and "Not verified".
- Commits are conventional (`type(scope): subject`). **No `Co-Authored-By` line and no AI attribution, ever.** Do not stage, commit or push unless asked. Never force-push a shared branch without an explicit yes.
- Look at a target before deleting or overwriting it. Keep changes scoped; do not add features, files or abstractions that were not asked for.
- Windows checkout uses CRLF (`core.autocrlf=true`). Edit files without rewriting every line ending.
- Docker image builds and `npm ci` need an unrestricted network. On a blocked network, build the API locally with `dotnet publish` and mount it into the runtime image.

## Skills (`.claude/skills/`)

| Skill | Use it for |
|---|---|
| `smartrail-modular-monolith` | Any backend change: modules, commands and queries, entities, migrations, endpoints, events |
| `smartrail-frontend` | Anything under `apps/web` |
| `smartrail-flutter` | Anything under `apps/mobile` |
| `spec-driven-development` | Writing, reviewing, implementing or archiving a spec |

## Commands (`.claude/commands/`)

`/verify` run the checks for what changed. `/feature-check` run them plus the browser flows and screenshots, and write a report (Markdown, HTML, PDF) to `reports/feature-check/`. `/review` read-only review of a target. `/commit` draft a commit message (never commits).

## Connectors

- `.mcp.json` declares a read-only GitHub connector (pull requests, Actions, code scanning) for reading CI and CodeQL results. It needs `GITHUB_MCP_TOKEN` in your environment, a fine-grained token with read access to this repository. The token is never stored in the repo.
- `playwright-cli` (skill in `.claude/skills/playwright-cli/`) drives a real browser for checks and screenshots. It cannot open `file:` pages.

## Where to read more

[README.md](README.md) · [CONTRIBUTING.md](CONTRIBUTING.md) · [specs/README.md](specs/README.md) · [docs/02-architecture/er-diagram.md](docs/02-architecture/er-diagram.md) · [docs/02-architecture/database-schema.md](docs/02-architecture/database-schema.md) · [docs/guides/ops-admin-console-api.md](docs/guides/ops-admin-console-api.md)
