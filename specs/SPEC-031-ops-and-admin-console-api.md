# SPEC-031 Controller, scheduler and administrator console API

| Field | Value |
|---|---|
| Status | Verifying |
| Owner | Malindu |
| Created | 2026-10-19 |
| Approved | 2026-10-19, Malindu ("fully complete the backend of admin and controller", "no device auth") |
| Branch | `feature/ops-admin-api` |
| Modules | Identity, Journeys, Operations, Network, Ticketing (Contracts only) |
| Apps | API (the web consoles and the driver app come next) |
| Changes | API contract, data model (new tables, append-only audit tables), permissions, seed data |

## 1. Summary

The back end behind the staff web console: the controller runs the day (live board, cancel, terminate short, interrupt, reinstate, platform changes, disruptions with a second approver, incidents, messages to drivers, audit), the scheduler plans it (timetable versions with a different approver, assigning units and drivers, units out of service, planned works), and the administrator controls who may do what (staff, roles, devices, security settings, access review). Every endpoint asks for one permission from the permission matrix. Everything the passenger app shows comes from here: a journey exists because the timetable generated it, it is late because the feed says so, it is cancelled or changes platform because a controller did it, a notice is on the Alerts screen because a controller published it, and a ticket turns Affected because its journey was cancelled.

The full endpoint list, the role matrix and the mobile-to-backend map are in [the guide](../docs/guides/ops-admin-console-api.md).

## 2. Traceability

| Kind | IDs |
|---|---|
| Requirements | REQ-OPS-001 to 013, 016 (014 punctuality report as CSV and JSON), REQ-LIVE-008, REQ-LIVE-017, REQ-SEC-003 |
| Use cases | UC-OPS-01 to 11, 13, 15, 16 (UC-OPS-12 station master, 14 data correction, 17 onward: not built, see section 4) |
| Specs absorbed | SPEC-010, SPEC-012 (without device authentication), SPEC-015, SPEC-027, SPEC-029; the permission tests of SPEC-017 |
| Cross-app flows | CF-02 (cancel a journey), CF-03 (platform change), CF-04 (disruption), CF-05 (planned works) |
| Design | Figma, HighFed, page "Ops" |

## 3. Decisions

- **One permission per endpoint** (`RequirePermission`). `journey.read` is the passenger and driver view and never opens the console; the console reads use `ops.read` (controller, scheduler, station master).
- **The administrator holds no operational permission.** The controller holds the Incident Manager and Reporting Analyst bundles, the scheduler the Fleet Controller bundle.
- **Second person, checked on the user, not the role:** a network-wide notice is approved by a controller who did not write it; a timetable version is approved by a scheduler who did not author it.
- **Reasons and audit:** every cancel, terminate short, interrupt, platform change, reinstate, assignment, notice action, incident action, message, timetable action and every access change needs a reason where the matrix says so and writes an append-only audit row (a database trigger refuses updates and deletes). The controller reads the operational and the access log, the administrator only the access log.
- **Concurrent edits:** journey actions carry the version the screen was looking at; a stale version answers 409 `Journeys.VersionConflict`.
- **Tickets follow the journey.** Ticket state is derived from the journey lifecycle, so cancelling or terminating short makes tickets Affected without a second write. The impact preview counts them before the controller confirms.
- **No device authentication** (Malindu, 2026-10-19). The device register holds name, type, station and last heartbeat only; there are no pairing codes or device tokens.
- **Staff accounts live in Keycloak.** The API holds no passwords and no role assignments of its own; it calls the Keycloak admin API with its own service account.

## 4. Scope

**In scope:** everything in the guide, section "Endpoints".

**Not built, on purpose or because another spec owns it**

| Item | Where it is owned |
|---|---|
| Server-side follow, inbox, notification event matrix (impact preview reports `followers: null`, not a fake 0) | SPEC-020 |
| Station master console: own-station notices, lift, stair and gate status, gate scan log | SPEC-028 |
| Driver duty, stop events, delay reasons, offline queue (only messages, acknowledgement and assistance are here) | SPEC-011 |
| Device sign-in, pairing, heartbeat intake | decided out of scope |
| Data correction of recorded events (UC-OPS-14), ghost journeys | not scheduled |
| Customer service agent, refunds view | deferred role, SPEC-024 |
| Calendar editing inside a timetable draft (services and times can be edited; days of the week are shared calendars) | not scheduled |

## 5. Verification

- Unit tests: disruption, incident, message and assistance rules; permission matrix (separation of duties); timetable version edits.
- Integration tests on a real PostgreSQL with migrations applied and signed test tokens: every console endpoint turns away a role without the permission (401 without a token); controller journey actions with stale versions; the network-wide notice with a second approver; incidents and the sign-out check; driver messages in their own scope; assistance on the dashboard; the timetable draft approved by a different scheduler; the administrator's staff, device and security flows; the demo seed on a fresh database, run twice.
- Architecture tests: modules talk only through Contracts.
