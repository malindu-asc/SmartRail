# SmartRail Ops console

React 19 + TypeScript + Tailwind (theme tokens) + TanStack Query. Controller, scheduler and administrator screens in one app; what a person sees depends on their permissions. Rules for changing it: `.claude/skills/smartrail-frontend/SKILL.md`. API contract: `docs/guides/ops-admin-console-api.md`.

## Run it

**With Docker (everything, including sign-in and the API):**

```bash
docker compose up -d --build api console
```

Open <http://localhost:3000>. You are sent to the sign-in page and back.

**For development (hot reload):** keep the stack above running, then

```bash
cd apps/web
npm install
npm run dev -w console
```

Open <http://localhost:3000> (stop the `console` container first, or it holds the port). Vite proxies `/api` to `http://localhost:5080`. Change the target with `VITE_API_PROXY`; the sign-in service defaults to `http://localhost:8080` (`VITE_KEYCLOAK_URL`, `VITE_KEYCLOAK_REALM`, `VITE_KEYCLOAK_CLIENT_ID`).

## Checks

```bash
cd apps/web/console
npm run lint && npm run typecheck && npm run test && npm run build
```

## Seeded staff accounts

Sign in with the email and the password from `infra/keycloak/smartrail-realm.json`.

| Email | Role | Lands on |
|---|---|---|
| admin@gmail.com | System administrator | Admin console |
| controller@gmail.com | Network controller | Dashboard |
| controller2@gmail.com | Network controller (second approver) | Dashboard |
| scheduler@gmail.com | Scheduler | Dashboard |
| scheduler2@gmail.com | Scheduler (second approver) | Dashboard |

Passenger and driver accounts (`passenger@gmail.com`, `driver@gmail.com`, `driver2@gmail.com`, `driver3@gmail.com`) are for the mobile apps; in the console they land on the no-access page.

A change to the realm file only applies to a new Keycloak volume (`docker compose down -v` wipes accounts made through the app).
