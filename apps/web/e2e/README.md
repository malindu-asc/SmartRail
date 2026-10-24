# SmartRail end-to-end flows

Five flows of the demo, run in a real browser (Playwright, Chromium) against the running Docker Compose stack. Nothing is mocked: each
flow signs in through Keycloak, and some of them act as the driver by calling the same API the driver app calls.

They are run by hand, not in CI. **They have not been run yet** (written 21 October 2026); the first run may need a selector fixed.

## Before you run them

1. The stack is up: `docker compose up -d` from the repository root (API on 5080, Keycloak on 8080, console on 3000).
2. Once only, from this folder: `npm install` and `npm run install:browser`.

The seeded accounts are read from `infra/keycloak/smartrail-realm.json`, so there is no password in these files. The addresses can be
changed with `CONSOLE_URL`, `KEYCLOAK_URL` and `API_URL`.

## Run them

```bash
npm run e2e               # all flows
npm run e2e:critical      # the five flows in tests/critical-flows.spec.ts
npm run e2e:headed        # watch the browser
npm run e2e:report        # open the HTML report of the last run
```

A failed run leaves a screenshot and a trace in `test-results/`; open a trace with `npx playwright show-trace <folder>/trace.zip`.

## The flows

| # | Flow | What it proves |
|---|---|---|
| 1 | Administrator invites a driver | A new account is created and listed (Users) |
| 2 | Who sees what | The administrator has no operations screens, the controller has no administration, and a direct address does not get round it |
| 3 | Controller publishes a disruption | The notice is on the controller's Active list and on the public list passengers read |
| 4 | Driver report to published notice | A report from a driver reaches Driver messages, and "Publish to passengers" turns it into a public notice |
| 5 | Message to a driver | The controller sends it, the driver acknowledges it, and the console shows Acknowledged |

## What they leave behind

Every run adds real rows (a user, notices, a message). Text is made unique per run, so runs can repeat. Reset the database with
`docker compose down -v` when you want a clean start.

## Not covered

The mobile apps (they are Flutter; their widget tests are in `apps/mobile`), the station displays, and the passenger alerts. Flow 3 and
flow 4 prove that a notice becomes public, which is what the passenger app reads.
