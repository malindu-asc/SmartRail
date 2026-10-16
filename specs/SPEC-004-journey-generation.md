# SPEC-004 Journey generation and the journey model

| Field | Value |
|---|---|
| Status | In progress |
| Owner | Malindu |
| Created | 2026-10-16 |
| Approved | 2026-10-16, Malindu (drafts accepted for Q1 to Q5) |
| Branch | `feature/journeys` |
| Pull requests | — |
| Modules | Journeys (owner), Network (two small Contracts additions), Identity (Scheduler role), BuildingBlocks (audit) |
| Apps | API |
| Changes | API contract, data model, seed data, background job, cross-module contracts, Keycloak realm |

**Pre-spec evaluation:** not trivial (new module content, a background job, new tables). Deliverable in about three days. It depends on SPEC-003 (the timetable and its Contracts interface) and the agreed schema in `docs/02-architecture/database-schema.sql`. It must come before live ingest, because live data attaches to journeys.

## 1. Summary

Turn the timetable into the things passengers actually follow: **journeys**. A nightly job creates a journey for every service that runs on each of the next 7 service dates, with all its stops and scheduled times in UTC. A journey has a lifecycle state machine, time-ranged vehicle and driver assignments, and a public scheduled read API (a journey by id, and a station board). A scheduler can add a manual special or relief journey. Live data, delays and confidence come in SPEC-005.

## 2. Motivation

- **Journey id is the identity** of a train run (REQ-LIVE-019). Tickets, follows, delays, platforms and notifications all attach to it. It has to exist, stable and unique per service and date, before any of them can.
- **The rules that decide whether the system lies are decided here:** generation must be idempotent, a new timetable version must never rewrite a generated journey (REQ-LIVE-017), and a service that passes midnight must stay on its own service date (REQ-LIVE-009).
- **Passengers and displays can already see something real** once this is built: a scheduled board by journey id, which SPEC-005 and SPEC-007 then make live.

## 3. Traceability

| Kind | IDs |
|---|---|
| Requirements | REQ-LIVE-001 (lifecycle), REQ-LIVE-002 (punctuality is separate; fields exist, filled in SPEC-005), REQ-LIVE-008 (nightly generation, manual path), REQ-LIVE-009 (service date), REQ-LIVE-010 (time-ranged assignment), REQ-LIVE-014 (platform per stop with state), REQ-LIVE-017 (versions never alter generated journeys), REQ-LIVE-019 (journey id is the identity), REQ-DSP-007 (row timing), REQ-OPS-011 (audit), REQ-NFR-005 (time), REQ-SEC-001 |
| Assumptions | ASM-026 (7-day horizon), ASM-017 (single time zone) |
| State models | `04-domain/state-models.md` sections 1a (lifecycle), 2 (journey stop), 3 (platform) |
| Edge cases | EDG-002 (23:50 to 00:35), EDG-003 (holiday), EDG-001 (new version while journeys exist), EDG-004, EDG-006 (generation and calendar cases) |
| Use cases | UC-OPS-05 (generation from a version), UC-PAX-01, UC-PAX-02 (scheduled data) |
| Schema | `docs/02-architecture/database-schema.sql`: `journeys` schema (journeys, journey_stops, vehicles, vehicle_assignments, crew_duties, driver_assignments, audit_entries) |
| Design | Passenger app: 07 Departures, 08 Journey |

## 4. Scope

**In scope**
- The Journeys module: domain, application, infrastructure, API.
- Journey generation: a background job and a re-runnable command, horizon 7 days, idempotent.
- The journey and journey-stop model with the full lifecycle transition table and stop and platform states.
- Vehicles, time-ranged vehicle assignments, crew duties and time-ranged driver assignments, with database rules against double booking.
- A demo fleet and roster, seeded on a flag, that assigns every seeded journey.
- Manual special or relief journey (Scheduler), audited.
- Public scheduled reads: a journey by id and a station board.
- Two small Contracts additions in Network (time zone, station names).
- A thin audit mechanism in the building blocks (`IAuditLog`), used here first and by later specs.
- A `scheduler` role, demo user and policy in the Keycloak realm.

**Not in this spec, and the spec that builds it**

| Item | Built in |
|---|---|
| Live ingest, delay, confidence, Running and later transitions driven by events | SPEC-005 |
| Realtime push and the live board contract | SPEC-007 |
| Cancel, terminate, platform change by a controller (the domain methods exist here, the endpoints and screens do not) | SPEC-010 |
| Assignment editing screens, vehicle unavailability, unassigned queue and alerts (REQ-LIVE-011, REQ-OPS-012) | SPEC-012 |
| Driver app duty view and stop events | SPEC-011 |

## 5. Detailed design

### 5.1 Domain model (Journeys module)

| Entity | Fields | Rules |
|---|---|---|
| Journey | id, service id, timetable version id, service date, train number (label), route code, origin and destination station codes, creation origin (Generated, Manual), manual reason, scheduled departure and arrival (UTC), lifecycle, running sub-state, current stop sequence, delay, punctuality band, confidence, last accepted update, version | Id is the identity. Scheduled times have no setter. Lifecycle changes only through the transition table. `Manual` needs a reason. Unique per (service, service date) when a service exists. |
| JourneyStop | journey id, sequence, station code, kind, state, scheduled arrival and departure (UTC), estimated and actual times, estimate source, dwell, platform code, platform state, original platform, changed at and by | Scheduled times never change. Stop state: Pending, Arrived, Departed, Skipped, Cancelled. Platform state: TBA, Assigned, Changed. |
| Vehicle | id, code, name, kind, active | Code unique. |
| VehicleAssignment | journey id, vehicle id, valid from, valid to | Time-ranged. One vehicle per journey at a time; a vehicle is not on two journeys at once (database exclusion constraints). A swap closes one row and opens the next, the journey id never changes. |
| CrewDuty | driver subject, service date, rostered window, state, signed on and off times, handover, review state | Window must be valid; a driver's windows cannot overlap. States Rostered, SignedOn, OnJourney, SignedOff, Orphaned (used from SPEC-011). |
| DriverAssignment | journey id, duty id, valid from, valid to | One duty on a journey at a time (so the outgoing duty must close before the incoming opens). |

**Lifecycle transitions** (from the state model; anything else is rejected and returns a modelled error):

| From | To |
|---|---|
| Planned | Ready, Cancelled, TerminatedEmergency |
| Ready | Planned, Running, Cancelled, TerminatedEmergency |
| Running | Completed, TerminatedShort, Interrupted, TerminatedEmergency |
| Interrupted | Running, TerminatedShort, Cancelled, TerminatedEmergency |
| Cancelled | Planned (reinstated, only before the scheduled departure, with a reason) |
| Completed, TerminatedShort, TerminatedEmergency | none |

`Planned` becomes `Ready` when both a vehicle and a driver are assigned, and goes back to `Planned` when either is removed. Public clients see `Planned` for both. `Running` has a sub-state `AtStop` or `InSection`.

### 5.2 Generation

For each service date from today (in the network time zone) through today + horizon − 1:

1. Ask `INetworkTimetable.GetServicesRunningOnAsync(date)` (SPEC-003).
2. For each service with no journey for (service id, date): create the journey and its stops in one transaction. Scheduled times are service-date midnight in the network time zone plus the stop's minutes, converted to UTC. Passing points are kept (working view).
3. Existing journeys are never touched, so a new timetable version changes only dates that have no journeys yet (REQ-LIVE-017).

A unique index on (service id, service date) makes concurrent runs safe; a violation means someone else created it and is ignored.

Run by a `BackgroundService` daily at a configured local time (`Journeys:Generation:RunAtLocalTime`, default `01:00`) and once at start-up when `Journeys:Generation:RunOnStartup` is true (Aspire, Compose, tests). Horizon: `Journeys:Generation:HorizonDays`, default 7.

### 5.3 Contracts

- Network.Contracts gains `INetworkTimeZone` (zone, today, local date, local time to UTC), `INetworkStations` (station code to localized name) and `INetworkRoutes` (ordered station codes of a route, used to check manual journeys). All three are implemented in Network.Infrastructure from existing code.
- Journeys.Contracts gets `IJourneyLookup { Task<JourneyRef?> FindAsync(Guid journeyId) }` for later modules (tickets, notifications), returning id, service date, lifecycle and stop codes. No logic.

### 5.4 Public API (anonymous, read-only, ETag, max-age 5 seconds)

| Method and route | Success | Errors |
|---|---|---|
| `GET /api/journeys/{id}?lang=` | `200` `{ journeyId, serviceDate, timeZone, trainNumber, routeCode, status, confidence, origin, destination, stops: [{ sequence, stationCode, stationName, state, scheduledArrivalUtc, scheduledDepartureUtc, platform, platformState }] }`. `status` is `Planned` for Planned and Ready. Passing points are removed and the sequence renumbered. Delay and estimates are `null` until SPEC-005 | `404` unknown id |
| `GET /api/journeys/boards/{stationCode}?count=10&lang=` | `200` `{ station, timeZone, asOfUtc, items: [{ journeyId, trainNumber, routeCode, destinationCode, destinationName, scheduledDepartureUtc, platform, platformState, status, confidence }] }`: the next departures from that station, sorted by scheduled time, across today's and the previous service date, so a post-midnight departure of yesterday's service still appears | `404` unknown station, `400` bad count (1 to 50) |

Board row timing (REQ-DSP-007), scheduled-only for now: a row disappears one minute after its scheduled departure; a Cancelled row stays 15 minutes after it. SPEC-007 makes the rows follow actual departures.

### 5.5 Manual special or relief journey

`POST /api/journeys/manual` (policy `Scheduler`): `{ routeCode, trainNumber, serviceDate, stops: [{ stationCode, arrivalTime, departureTime, platform }], reason }` (times as `HH:mm`, past 24:00 allowed). Validated like a timetable service (stops follow the route in order, times do not go backwards). Creates a journey with `creation_origin = Manual`, no service id, and a mandatory reason, and writes an audit entry in the same transaction. Returns `201` with the journey id; `403` for other roles; `401` anonymous.

### 5.6 Audit

`IAuditLog` (Application.Core) with an `AuditEntry` mapped in Infrastructure.Core and a per-module table `audit_entries` (the shape in the schema). It writes in the module's unit of work, so the change and its audit row commit together. The table is append-only (trigger). Journeys is the first module to use it; Identity, Network, Operations and Ticketing adopt it in their own specs.

### 5.7 Data

Tables exactly as in `database-schema.sql` (`journeys` schema): `journeys`, `journey_stops`, `vehicles`, `vehicle_assignments`, `crew_duties`, `driver_assignments`, `audit_entries`, plus the module outbox. The first migration creates the `btree_gist` extension. `vehicle_unavailability`, `journey_events`, `telemetry_readings`, `journey_delay_reasons`, `duty_location_samples` and `punctuality_daily` are created by the specs that first need them (SPEC-005, 011, 012). Cross-module references are codes and ids with no foreign keys.

### 5.8 Demo fleet and roster (flag `Journeys:Seed:DemoFleet`, default off)

- 8 trainsets, one per route and direction (so no vehicle overlaps itself).
- A duty roster per vehicle chain per service date: an early shift (05:30 to 14:00) and a late shift (14:00 to 00:30 next day). One early-shift duty is held by the demo driver user from the Keycloak realm; the others use placeholder subjects.
- After each generation run, every journey without an assignment is assigned the vehicle for its route and direction and the duty that covers its time, so it becomes `Ready`. Insert-only and idempotent.

### 5.9 Keycloak

Add realm role `scheduler`, a demo user `scheduler` with a fixed id, and policy `Scheduler` (constants in `Roles`/`Policies`). Tests extend the role and policy matrix.

### 5.10 Rules, security, observability

- Times are UTC in storage and in the API; the network time zone is returned with every response (REQ-NFR-005). The clock is injected.
- Public endpoints expose no driver or vehicle identity, no assignments and no ids except the journey id.
- Generation logs counts created and skipped per date; a metric counts generated journeys and failures; a missing timetable for a date is logged at information level.

## 6. Acceptance criteria

- [ ] **AC-1** Given the seeded timetable, when generation runs, then each of the next 7 service dates has one journey per running service, with service id, version id, train number, route, origin, destination, lifecycle `Planned`, confidence `Scheduled` and every stop (including passing points) with scheduled UTC times computed in the network time zone.
- [ ] **AC-2** Running generation again creates nothing and changes nothing.
- [ ] **AC-3** A date on which a calendar exception removes the weekday services gets no journeys for them.
- [ ] **AC-4** A service that passes midnight belongs to its own service date; with the clock at 00:05 local the next day, the station board still lists its post-midnight departure.
- [ ] **AC-5** After a new timetable version is published with a later effective date, existing journeys are unchanged and only dates without journeys use the new version.
- [ ] **AC-6** The lifecycle transition table is exhaustive: for all pairs of the eight states, exactly the transitions in 5.1 are allowed and the rest return a modelled error.
- [ ] **AC-7** A journey becomes `Ready` only when it has both a vehicle and a driver and returns to `Planned` when either is removed.
- [ ] **AC-8** A Planned or Ready journey can be cancelled with a reason; a Cancelled journey can be reinstated to `Planned` only before its scheduled departure.
- [ ] **AC-9** The database refuses overlapping vehicle assignments for one journey or one vehicle, and overlapping driver assignments for one journey; swapping a vehicle by closing one row and opening the next works and the journey id is unchanged.
- [ ] **AC-10** No domain operation changes a scheduled time.
- [ ] **AC-11** A Scheduler can create a manual journey (reason required, route order and times validated), and an audit entry is written in the same transaction; other roles get `403`, anonymous `401`.
- [ ] **AC-12** `GET /api/journeys/{id}` returns the public view (passing points hidden, sequence renumbered, `Planned` for Planned and Ready, delay `null`); an unknown id returns `404`.
- [ ] **AC-13** The station board lists the next departures sorted by scheduled time, hides a row one minute after its scheduled departure, keeps a Cancelled row for 15 minutes, and validates `count`.
- [ ] **AC-14** The demo seed adds 8 vehicles, the roster and the assignments so that all seeded journeys are `Ready`; repeating it changes nothing; with the flag off nothing is seeded.
- [ ] **AC-15** The audit table accepts inserts and refuses update and delete.
- [ ] **AC-16** The architecture tests pass: Journeys references Network only through Contracts.

## 7. Test plan

| AC | Type | Location |
|---|---|---|
| AC-6, 7, 8, 10 | Unit (state machine with all 64 pairs, assignment and cancel rules) | `tests/SmartRail.Modules.Journeys.Tests/` (new project) |
| AC-13 row timing | Unit with a fixed clock | same |
| AC-1 to AC-5 | Integration: real Postgres, generation run with a fixed clock and a published second version | `tests/SmartRail.Api.IntegrationTests/Journeys/` |
| AC-9, AC-15 | Integration, direct inserts | same |
| AC-11, AC-12, AC-13, AC-14 | Integration through the API (token issuer for Scheduler) | same |
| AC-16 | Architecture | existing `ModuleBoundaryTests` |

## 8. Dependencies

SPEC-003 (timetable and `INetworkTimetable`), the agreed schema, the Keycloak realm and token test issuer (SPEC-001).

## 9. Risks

| Risk | Likelihood | Mitigation |
|---|---|---|
| Time conversion error across the day boundary | Medium | Tests on 23:50 and 00:05 with a fixed clock; scheduled values stored in UTC once |
| Concurrent generation runs | Low | Unique index and tolerant insert |
| Demo roster looks artificial | Low | Documented as demo data; flag off by default |
| Exclusion constraints surprise EF | Low | Created in the migration SQL and covered by tests |

## 10. Design decisions

| # | Decision | Why |
|---|---|---|
| D1 | Scheduled times are stored in UTC on the journey, computed once at generation | A timetable edit can never move a generated journey (REQ-LIVE-017) and no time zone maths is needed at read time |
| D2 | Journeys copy the working stops, including passing points | The journey is the aggregate (identity model); passing points are needed for live position and are hidden only in the public view |
| D3 | The full lifecycle table lives in the domain now | State models require exhaustive tests; later specs only call transitions |
| D4 | Ready is derived from assignments, not set by hand | The state model defines it that way and it cannot drift |
| D5 | One generation path for the job, the startup run and tests | One tested code path |
| D6 | Cross-module data comes through Contracts, never tables | Module boundaries; the schema has no cross-schema keys |
| D7 | A thin audit mechanism is built now | Every operator change must be audited (REQ-OPS-011); building it once avoids five copies |
| D8 | The board and the journey endpoint exist before live data | The first passenger screen can be built against real scheduled data while SPEC-005 is written |

## 11. Open questions

None. Answered on 2026-10-16 with the drafts below.

- **Q1.** Does the nightly job run at 01:00 network time, and also once at start-up in development, Compose and tests? Draft: yes.
- **Q2.** Is the demo fleet and roster as in 5.8 (8 trainsets, early and late shifts, the demo driver on one early duty)? Draft: yes.
- **Q3.** Is `Ready` set as soon as both a vehicle and a driver are assigned (as the state model says)? Draft: yes.
- **Q4.** Do the unassigned alerts and queue (REQ-LIVE-011) stay in SPEC-012? Draft: yes.
- **Q5.** Add the `scheduler` role, demo user and policy to Keycloak now, because the manual path needs it? Draft: yes.

## 12. Tasks

- [x] Contracts: `INetworkTimeZone`, `INetworkStations`, `INetworkRoutes` (Network), `IJourneyLookup` (Journeys)
- [x] Building block: `IAuditLog`, `AuditEntry`, append-only trigger helper
- [x] Domain: Journey, JourneyStop, lifecycle transition table, Vehicle, assignments, CrewDuty
- [x] Application: generation command, manual journey command, journey and board queries
- [ ] Application: assignment commands (assign, replace, unassign vehicle and driver; the domain rules and tables exist, the commands and endpoints belong with the planning screens)
- [x] Infrastructure: mappings, migration (with `btree_gist` and exclusion constraints), generator service, background worker, readers, demo seeder
- [x] API: journey, board and manual endpoints
- [x] Keycloak: scheduler role, user, policy; update tests and README
- [x] Tests for every AC; update the anonymous allow-list
- [x] README: endpoints and settings

## 13. Review record

| Date | Reviewer | Decision | Notes |
|---|---|---|---|

## 14. Verification

| AC | Result | Evidence |
|---|---|---|
| AC-1 to AC-16 | Automated tests pass (Journeys unit 77, API integration 13 new, architecture 25) | Manual run in Docker/Aspire still to do; assignment commands are not built yet |

## 15. Sync status

- [ ] Requirements or use cases updated if this spec changed them
- [ ] ADR created or updated
- [ ] README or developer docs updated
- [ ] Traceability regenerated
- [ ] Register in `specs/README.md` updated

## 16. Change log

| Date | Change | Approved by |
|---|---|---|
| 2026-10-16 | Drafted | — |
| 2026-10-16 | Approved by the owner | Malindu |
