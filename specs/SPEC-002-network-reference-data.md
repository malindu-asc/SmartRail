# SPEC-002 Network reference data and seed

| Field | Value |
|---|---|
| Status | Verifying |
| Owner | Malindu |
| Created | 2026-10-16 |
| Approved | 2026-10-16, Malindu (answers to Q1 to Q3: drafts accepted) |
| Branch | `feature/network-reference-data` |
| Pull requests | — |
| Modules | Network |
| Apps | API |
| Changes | API contract, data model, seed data |

**Pre-spec evaluation:** not trivial (new tables, public endpoints, seed). Touches only the Network module and the API host. Deliverable in about three days. No existing spec covers it.

## 1. Summary

Give SmartRail a network to talk about: stations, platforms and routes with ordered stops, loaded from an idempotent seed and readable through a public API in the passenger's language. Every later feature (journeys, boards, tickets, simulator, apps) starts from these identifiers.

## 2. Motivation

Nothing else can be built without stable station, platform and route ids. The identity model fixes the rule: stations, platforms and routes live for years and Network owns them. This spec builds that foundation and nothing that depends on timetables or live operations.

## 3. Traceability

| Kind | IDs |
|---|---|
| Requirements | REQ-PAX-013 (two languages), REQ-LIVE-007 (ETA uses offsets and dwell, shapes the stored times), REQ-LIVE-009 (stop times may pass 24:00), REQ-LIVE-014 (platform per stop), REQ-LIVE-019 (identity), REQ-SEC-001 (anonymous posture), REQ-INT-001 (catering data is owned by the provider) |
| Assumptions | ASM-017 (fictional seed network, about 12 stations, single time zone) |
| Domain | `04-domain/identity-model.md` (Station, Platform, Route; working versus public view) |
| Design | Passenger app: 18 Find a station, 19 Station on map, 10 Plan a journey |

## 4. Scope

**In scope**
- Languages, stations with translated names, facilities, platforms.
- Routes with ordered stops, planned running times, dwell and stop kind.
- Read-only public API for stations and routes, localized.
- Insert-only reference seeder (12 stations, 4 routes), switched on by configuration.
- Public projection that hides passing points.

**Out of scope** (and why)
- Editing the network through the API (later spec: scheduler and admin console).
- Services, timetables, versions, calendars, journey generation (own spec; this one only supplies stations and routes).
- Direction of travel (belongs to the service, see decision D1).
- Network time zone setting (arrives with timetables, which are the first thing that needs it).
- Distances and coordinates (see decision D3).
- Catering availability (see decision D2).
- Rate limiting (REQ-SEC-002 is a cross-cutting spec for all public endpoints).
- Database roles and a separate migration step (see decision D5).

## 5. Detailed design

### 5.1 Domain model (Network module)

| Entity | Fields | Rules |
|---|---|---|
| Language | code, name, is_default | Exactly one default. Seeded: `en` (default), `si`. More can be added by data alone. |
| Station | id, code, name, facilities, is_active | Code is 3 to 5 upper-case letters, unique, immutable. `name` is the default-language name. |
| StationTranslation | station_id, language_code, name | Only for non-default languages. One per station and language. |
| Platform | id, station_id, code, is_available | Code unique within the station. |
| Route | id, code, name, is_active | Code unique. A route is an ordered line; it is bidirectional, direction belongs to the service. |
| RouteStop | route_id, sequence, station_id, default_platform_id, minutes_from_previous, dwell_minutes, kind | Sequence 1..n with no gaps. A station appears once per route. Default platform must belong to the stop's own station. First stop has 0 minutes from previous. |

Stop kinds: `Stop`, `PickUpOnly`, `SetDownOnly`, `Passing`. Passing points exist in the working view only.
Facility codes: `StepFree`, `Lifts`, `Toilets`, `TicketOffice`, `AssistancePoint`.

### 5.2 API contract

All endpoints are anonymous, read-only, and send `ETag` and `Cache-Control: public, max-age=300`. Language comes from `?lang=` or the `Accept-Language` header; an unknown language falls back to the default. Responses carry `language`, the language actually used.

| Method and route | Success | Errors |
|---|---|---|
| `GET /api/network/stations` | `200` `{ language, items: [{ code, name, facilities }] }` sorted by localized name; `?q=` filters by name or code (case-insensitive, 2 or more characters) | `400` if `q` is 1 character |
| `GET /api/network/stations/{code}` | `200` `{ code, name, facilities, platforms: [{ code, isAvailable }] }` | `404` unknown or inactive |
| `GET /api/network/routes` | `200` `{ items: [{ code, name, stopCount }] }`, `stopCount` counts public stops | none |
| `GET /api/network/routes/{code}` | `200` `{ code, name, stops: [{ sequence, stationCode, stationName, defaultPlatform, minutesFromPrevious, dwellMinutes, kind }] }`, passing stops removed, sequence renumbered | `404` unknown or inactive |

Errors use the existing ProblemDetails shape. Lookups by code are case-insensitive.

### 5.3 Events

None. Network data is read, not published. Change events arrive with the editing spec.

### 5.4 Data

Schema `network`, per-module migrations history (existing convention). Names are snake_case.

| Table | Columns | Database-enforced rules |
|---|---|---|
| `languages` | code pk, name, is_default | one row with `is_default` (partial unique index) |
| `stations` | id uuid pk, code, name, facilities text[], is_active, created_at, updated_at | unique(code), check(code ~ '^[A-Z]{3,5}$') |
| `station_translations` | station_id fk, language_code fk, name | pk(station_id, language_code) |
| `platforms` | id uuid pk, station_id fk, code, is_available | unique(station_id, code), unique(id, station_id) |
| `routes` | id uuid pk, code, name, is_active | unique(code) |
| `route_stops` | route_id fk, sequence, station_id fk, default_platform_id null, minutes_from_previous, dwell_minutes, kind | pk(route_id, sequence), unique(route_id, station_id), composite fk(default_platform_id, station_id) to platforms(id, station_id), check(sequence >= 1), check(minutes_from_previous >= 0), check(dwell_minutes >= 0), check(kind in (...)) |

The composite foreign key makes it impossible for a stop to point at another station's platform. Audit columns come from the existing auditable interceptor. Enums are stored as text with check constraints.

### 5.5 Seed

- `NetworkReferenceSeeder` inserts a row only if its natural key (`code`) is missing. It never updates or deletes, so anything edited later through the console survives a restart.
- It runs at startup only when `Network:Seed:ReferenceData` is `true`. The default is `false`. The Aspire AppHost and Docker Compose set it to `true` (they also apply migrations); nothing guesses from the environment name.
- Fixed ids for seeded rows, so every environment has the same ids.
- It runs after migrations and in one transaction. It logs how many rows it added and how many were already present.

Seed network (fictional, ASM-017):

| Route | Stops in order |
|---|---|
| `MAIN` Central to Riverside | Central, Oak Park, Mill Lane, Riverside |
| `HILL` Central to Hill Town | Central, Oak Park, Hill Town, Riverside, Market Street |
| `COAST` Central to Bay View | Central, Harbour, Lakeside, Bay View |
| `NORTH` Market Street to Airport | Market Street, North Gate, Green Hill, Airport |

Harbour is a passing point on `COAST` (it exists in the working view, never in the public one). Twelve stations, one to three platforms each, Sinhala names for every station, step-free at Central, Riverside, Hill Town and Airport. Running times and dwell are rounded demo values.

### 5.6 Business rules

1. A route is valid only if its stops are consecutive, unique per station and have non-negative times.
2. Inactive stations and routes are never returned.
3. The public view never returns `Passing` stops and renumbers the sequence.
4. Callers use codes; internal ids are not exposed.
5. A translation is never stored for the default language; the default name lives on the station.
6. The localized name falls back to the default name when no translation exists.

### 5.7 Security and permissions

Network endpoints are explicitly `AllowAnonymous` because the API requires authentication by default. No personal data. Nothing is audited because nothing is written.

### 5.8 Observability

Existing request traces and metrics apply. The seeder logs counts added and present.

## 6. Acceptance criteria

- [x] **AC-1** Given the seeded database, when I `GET /api/network/stations`, then I receive all 12 active stations sorted by name, each with code, name and facilities.
- [x] **AC-2** Given `?q=oak`, then only stations whose name or code contains "oak" (any case) are returned; `?q=o` returns `400`.
- [x] **AC-3** Given a valid code in any case, when I `GET /api/network/stations/{code}`, then I receive the station with its platforms; unknown or inactive returns `404`.
- [x] **AC-4** Given `lang=si` or `Accept-Language: si`, then names are Sinhala and `language` is `si`; an unknown language returns the default names and `language: "en"`.
- [x] **AC-5** Given route `HILL`, when I `GET /api/network/routes/HILL`, then stops are in sequence with times and kinds; unknown returns `404`.
- [x] **AC-6** Given a route with a `Passing` stop, when read publicly, then it is absent and sequence numbers are contiguous.
- [x] **AC-7** Given the seed has already run, when it runs again, then no row is duplicated or changed, including a station name edited in the meantime.
- [x] **AC-8** Given `Network:Seed:ReferenceData` is not set, when the app starts, then nothing is seeded.
- [x] **AC-9** Given no token, when I call a network endpoint, then it succeeds; any other API endpoint still returns `401`.
- [x] **AC-10** Given invalid route data (gap in sequence, duplicate station, negative time), then a domain error is returned and nothing is saved.
- [x] **AC-11** Given a route stop whose default platform belongs to another station, when I try to save it, then the database refuses it.
- [x] **AC-12** Given the architecture tests run, then Network references no other module's internals.

## 7. Test plan

| AC | Type | Location |
|---|---|---|
| AC-1 to AC-9 | Integration: API factory with a real Postgres (Testcontainers), new database per test class | `tests/SmartRail.Api.IntegrationTests/Network/` |
| AC-10 | Unit | `tests/SmartRail.Modules.Network.Tests/` (new project) |
| AC-11 | Integration, direct insert | `tests/SmartRail.Api.IntegrationTests/Network/ConstraintTests.cs` |
| AC-12 | Architecture | existing `ModuleBoundaryTests` |

## 8. Dependencies

- Authentication foundation (secure-by-default fallback policy), already built.
- Existing `network` schema, `NetworkDbContext` and module registration.
- New test package: Testcontainers for PostgreSQL. It must pass the vulnerable-package check.

## 9. Risks

| Risk | Likelihood | Mitigation |
|---|---|---|
| Two languages are not the final pair | Medium | Languages are data; adding Tamil is a seed row and translations |
| Timetable spec needs more from routes | Medium | Routes stay a plain ordered line; services reference them by code |
| Seeder runs in an environment that should stay empty | Low | Default off, explicit flag, covered by AC-8 |

## 10. Design decisions: what we took from the reference project and how ours is better

The reference project's database spec gave us the starting ideas. Each row says what the idea was, what our scenario actually needs, and the version we chose. Sources are in this repo.

| # | Topic | Idea taken from the reference | Our version | Why it is better for us |
|---|---|---|---|---|
| D1 | Line and direction | Keep the physical line separate from the train service; direction is a service property | A route is a bidirectional ordered line; direction belongs to the service | No duplicated reverse routes, and it fits our identity model where a service is per timetable version |
| D2 | Station names | Default name on the station plus a translation table | Same shape, but languages are data, only non-default languages are translated, and the API picks the language from `?lang=` or `Accept-Language` with a fallback | REQ-PAX-013 does not name the two languages. Nothing is hard-coded, and the default name has one source |
| D3 | Stop times | Scheduled arrival and departure as time of day | Minutes from the previous stop plus dwell | REQ-LIVE-009 needs stops past 24:00, REQ-LIVE-007 computes ETA from offsets and dwell, and no time zone has to be hard-coded (ASM-017) |
| D4 | Platform integrity | Database-level link so a stop's platform belongs to its own station | Same rule, plus an `is_available` flag and a test that attempts the invalid insert | REQ-LIVE-014. A wrong platform shown to passengers must be impossible, not just unlikely |
| D5 | Facilities | Catering flag and phone on the station | Facility set (step-free, lifts, toilets, ticket office, assistance point); catering is not stored | REQ-INT-001: the catering provider owns that data. Storing it would drift and break "no catering, no food row" |
| D6 | Distance | Distance in km along the line | Not stored now | REQ-LIVE-007 does not use it; add it when the simulator needs it |
| D7 | Reference data | Seeded inside the migration with fixed ids | Insert-only seeder with fixed ids, run on a flag | Rows are inserted if missing and never changed, so later console edits survive. The migration stays schema-only |
| D8 | Switches | Explicit configuration flags, default off | Same idea: `Network:Seed:ReferenceData` default off, on in development and Compose | Matches our existing `Database:ApplyMigrationsOnStartup` switch. Nothing guesses from the environment name |
| D9 | Passing points | Not modelled | Stop kind `Passing`, hidden from the public view | The identity model separates working and public timetables; the reference has no such split |
| D10 | Tests | One fresh database per test class | Same, via Testcontainers, plus a direct insert test for the database rules | Proves the constraints, not only the API |
| D11 | Migrations and roles | Runtime role and admin role | Out of scope here; separate spec with a one-shot migrator step for all modules | The API process should not hold admin credentials; this is cross-cutting |
| D12 | Calendar | Free-text service days | Out of scope; timetable spec models a real calendar with exception dates | REQ-LIVE-008 needs holiday exceptions |
| D13 | Schema layout | One schema and one context | Schema and context per module | Our modular monolith and boundary tests |

## 11. Open questions

None. Answered on 2026-10-16: second language is Sinhala (Q1); seed is 12 stations and 4 routes, growing with the timetable spec (Q2); facility list as drafted (Q3).

## 12. Tasks

- [x] Domain: Language, Station, StationTranslation, Platform, Route, RouteStop, rules and errors
- [x] Application: queries (stations, station by code, routes, route by code) and DTOs in Contracts
- [x] Infrastructure: EF mappings, migration, read queries, seeder
- [x] API: four endpoints, anonymous, language handling, ETag and cache headers
- [x] Tests for every AC
- [x] Update README endpoint list (traceability regeneration pending)

## 13. Review record

| Date | Reviewer | Decision | Notes |
|---|---|---|---|

## 14. Verification

| AC | Result | Evidence |
|---|---|---|
| AC-1 | Pass | `NetworkApiTests.Lists_all_twelve_stations_sorted_by_name` |
| AC-2 | Pass | `NetworkApiTests.Search_filters_by_name_or_code_and_rejects_one_character`; `QueryHandlerTests` |
| AC-3 | Pass | `NetworkApiTests.Station_is_found_by_code_in_any_case_with_platforms` |
| AC-4 | Pass | `NetworkApiTests.Names_follow_the_requested_language_with_fallback`; `QueryHandlerTests.Unknown_language_falls_back_to_the_default` |
| AC-5 | Pass | `NetworkApiTests.Route_comes_back_in_sequence_with_times_and_kinds` |
| AC-6 | Pass | `NetworkApiTests.Passing_points_are_not_in_the_public_route`; `QueryHandlerTests.Passing_points_are_hidden_and_the_sequence_is_renumbered` |
| AC-7 | Pass | `NetworkSeedTests.Seeding_again_adds_nothing_and_never_overwrites_edits` |
| AC-8 | Pass | `NetworkSeedOffTests.Nothing_is_seeded_unless_the_flag_is_on` |
| AC-9 | Pass | `NetworkApiTests.Network_is_anonymous_but_everything_else_still_needs_a_token`; `SecureByDefaultTests` allow-list updated |
| AC-10 | Pass | `RouteTests`, `StationTests` |
| AC-11 | Pass | `NetworkConstraintTests.Database_refuses_a_default_platform_from_another_station` |
| AC-12 | Pass | `ModuleBoundaryTests` (25 tests) |

Manual run on 2026-10-16: the real API against a fresh Postgres 16 with migrations and seed on. First start added 2 languages, 12 stations, 21 platforms and 4 routes; a second start added 0 (18 already present). Station search in Sinhala, the `COAST` route without its passing point, a 404 for an unknown station and a 401 for `/api/identity/whoami` without a token all behaved as specified.

Run on 2026-10-16: build with `-warnaserror` clean; 28 unit, 25 architecture and 35 API tests pass against Postgres 16 in Docker.

## 15. Sync status

- [ ] Requirements or use cases updated if this spec changed them
- [ ] ADR created or updated
- [x] README or developer docs updated
- [ ] Traceability regenerated
- [ ] Register in `specs/README.md` updated

## 16. Change log

| Date | Change | Approved by |
|---|---|---|
| 2026-10-16 | Drafted | — |
| 2026-10-16 | Approved by the owner; Q1 to Q3 answered with the drafts | Malindu |
| 2026-10-16 | Revision 2: ideas from the reference project adopted where they fit, adapted for our requirements (see section 10) | — |
