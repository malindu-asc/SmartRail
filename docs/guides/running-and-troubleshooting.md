# Running and troubleshooting

How to start, test and fix the SmartRail stack on a developer machine (Windows, PowerShell or Git Bash).

## 1. Prerequisites check

```bash
dotnet --version     # 10.0.x
docker --version     # Docker Desktop must be RUNNING
node -v              # 24.x
flutter --version    # 3.44 or newer
```

## 2. Start the stack

### Aspire (development)

```bash
dotnet run --project src/Host/SmartRail.AppHost
```

Leave the terminal open. It prints the dashboard link (usually `https://localhost:17063`). Wait until all resources are green. `keycloak` takes the longest on first start.

Add the Grafana stack:

```bash
dotnet run --project src/Host/SmartRail.AppHost -- --Observability:Enabled=true
```

Grafana: `http://localhost:3002` (`admin` / `admin`) then **Dashboards, SmartRail, SmartRail platform**. In this mode API telemetry goes to Grafana, so the Aspire Traces and Metrics pages stay empty for the API.

### Docker Compose (production-shaped)

```bash
cp .env.example .env
docker compose up -d --build
docker compose ps
```

Stop it with `docker compose down` (add `-v` to delete data volumes).

Do not run both at once: ports such as 8080 clash.

## 3. Ports

| Port | Compose | Aspire |
|---|---|---|
| 5080 | API | dynamic (see dashboard) |
| 8080 | Keycloak (http) | Keycloak (https) |
| 3000 / 3001 | Console / Kiosk | dynamic |
| 4000 | Catering | dynamic |
| 3002 | Grafana | Grafana (with the flag) |
| 9090 | Prometheus | Prometheus (with the flag) |
| 4317 | OTLP collector | OTLP collector (with the flag) |
| 17063 | - | Aspire dashboard |

## 4. Tests

```bash
dotnet test
```

Runs the architecture tests: Domain has no framework dependencies, layers point inward, modules touch each other only through `Contracts`.

```bash
npm run build --workspace=console --prefix apps/web
npm run build --workspace=kiosk --prefix apps/web
npm run typecheck --prefix services/catering
```

```bash
cd apps/mobile
flutter pub get
flutter analyze apps/passenger apps/driver
cd apps/passenger && flutter test
```

## 5. Smoke checks

1. Keycloak: open `/realms/smartrail/account` on the Keycloak URL and sign in as `controller`.
2. API: `/health` returns `Healthy` (Development).
3. Catering: `/health` returns `{"status":"ok"}`.
4. Grafana: the **SmartRail platform** dashboard loads. Panels fill once the API receives requests.

## 6. Database migrations

Per module, from the repository root (replace `<Module>`):

```bash
dotnet ef migrations add <Name> \
  --project src/Modules/<Module>/SmartRail.Modules.<Module>.Infrastructure \
  --context <Module>DbContext \
  --output-dir Persistence/Migrations
```

Migrations are applied on startup in Development only.

## 7. Troubleshooting

| Symptom | Cause and fix |
|---|---|
| `dotnet run` prints "Building..." and returns, nothing runs | Aspire socket folder permissions. Check the newest log in `%USERPROFILE%\.aspire\logs`. Fix: `Remove-Item -Recurse -Force "$env:USERPROFILE\.aspire\cli\bch"`, then run again |
| Build error "file is locked by SmartRail.AppHost" | The AppHost is still running. Press Ctrl+C in its terminal first |
| Resources stay `Waiting` | A dependency is not healthy yet. Check `keycloak` and `postgres` logs in the dashboard |
| Keycloak `Unhealthy` | Usually still starting. Wait a minute; if it persists open its log |
| Docker errors "cannot connect to the daemon" | Start Docker Desktop and wait until it reports running |
| Port already in use | Another stack is running. Stop Aspire or `docker compose down` |
| Git asks which GitHub account to use | `git config --global credential.https://github.com.username <your-user>` |
| `flutter pub get` complains about workspace | Run it from `apps/mobile`, not from a package folder |
| Grafana panels are empty | No traffic yet, or the API is not exporting. Hit the API a few times and check the collector container log |

## 8. Cleaning up

```bash
docker compose down -v        # compose stack and its volumes
docker ps -a                  # find leftover containers
dotnet clean SmartRail.slnx
```
