# SPEC-003 Timetable: versions, calendars and services

| Field | Value |
|---|---|
| Status | Verifying |
| Owner | Malindu |
| Created | 2026-10-16 |
| Approved | 2026-10-16, Malindu (drafts accepted for Q1 to Q3) |
| Branch | `feature/timetable` |
| Pull requests | — |
| Modules | Network (owner), Journeys (consumer through Contracts only) |
| Apps | API |
| Changes | API contract, data model, seed data, cross-module contract |

**Pre-spec evaluation:** not trivial. Touches only Network and adds one interface to its Contracts project. Deliverable in about three days. No existing spec covers it. It must come before journey generation, because a journey is one run of a service on one service date and nothing downstream can exist without services.

## 1. Summary

Give SmartRail a timetable: versioned sets of services (a route, a calendar, ordered stops with scheduled times), a calendar with exception dates, and the rule that decides which version applies on a given date. Passengers can read the scheduled timetable without signing in. The Journeys module gets a Contracts interface so the next spec can generate journeys without touching Network internals.

## 2. Motivation

- **Journeys need it.** Journeys are generated nightly from service × calendar (REQ-LIVE-008). Without services there is nothing to generate, no stop times to delay and nothing for a board to show.
- **The passenger app needs a fallback.** When a journey is Unknown or the phone is offline, the app shows the timetable (REQ-PAX-005, REQ-PAX-010). That data has to exist and be public (REQ-PAX-001).
- **Mistakes here are expensive.** Midnight services, holidays and "a new timetable must not rewrite a running journey" are the three classic ways a rail system lies. They are cheap to get right in the data model and very expensive to retrofit.

## 3. Traceability

| Kind | IDs |
|---|---|
| Requirements | REQ-LIVE-008 (generation needs service × calendar), REQ-LIVE-009 (service date, times past 24:00), REQ-LIVE-017 (changes apply from the effective date), REQ-LIVE-019 (train number is a label), REQ-OPS-003 (versions are immutable once published; author is not the approver), REQ-PAX-001 (timetable is public), REQ-NFR-005 (time handling), REQ-SEC-001 (anonymous posture) |
| Assumptions | ASM-017 (about 12 stations, 6 routes, about 40 services, one time zone), ASM-026 (7-day generation horizon, used by the next spec) |
| Edge cases | EDG-001 (new version while old journeys run), EDG-002 (23:50 departure arriving after midnight), EDG-003 (public holiday exception), EDG-005 (a bad version is superseded, never edited) |
| Use cases | UC-OPS-05 (versions, only the model and rules here), UC-PAX-01 and UC-PAX-02 (scheduled data for boards and timelines) |
| Domain | `04-domain/identity-model.md` (Service, working versus public timetable), `04-domain/glossary.md` |
| Design | Passenger app: 06 Home, 07 Departures, 08 Journey |
| Schema | `docs/02-architecture/database-schema.md` (the `network` schema is defined there) |

## 4. Scope

**In scope**
- Timetable versions with the states Draft, InReview, Published, Superseded and an effective date.
- Domain rules for versions: a Published version is immutable; the approver is not the author; publishing needs an effective date.
- Calendars (days of the week, valid range) with exception dates (Removed for holidays, Added for specials).
- Services: route, calendar, train number (a label), direction, ordered stops with explicit scheduled arrival and departure, stop kind and default platform.
- The rule "which version applies on date D".
- A Contracts interface for Journeys: services running on a service date, including passing points.
- Public read endpoints: a station's scheduled departures or arrivals for a date, and one service's stops for a date.
- Network time zone setting.
- Insert-only seed: one Published version, 3 calendars with a holiday, about 40 services including a midnight-crossing one.

**Not in this spec, and the spec that builds it.** These are all MVP items (`11-scope/mvp-scope.md`); they are only split into their own specs because each is large.

| Item | Why it is not here | Built in |
|---|---|---|
| Editing, submitting, approving and publishing a version (API and console) | Needs auth rules, screens and audit; this spec builds the data and the rules it will enforce | SPEC-015 |
| Generating journeys, plus the manual path for special and relief services | Needs this spec's data first | SPEC-004 |
| Vehicle and driver assignment, unassigned queue | Needs journeys to assign to | SPEC-012 |
| Journey search A to B with one transfer (REQ-PAX-006) | Needs services and live delay; a read feature on top of this data | SPEC-016 |
| Public rate limiting (REQ-SEC-002) and the permission-matrix tests (REQ-SEC-003) | Applies to every public endpoint, not only the timetable | SPEC-017 |

Until those specs land, the demo uses the seeded timetable (read-only), which is what every later spec reads.

## 5. Detailed design

### 5.1 Domain model (Network module)

| Entity | Fields | Rules |
|---|---|---|
| TimetableVersion | id, label, status, effective_from, author, approved_by, published_at | Status moves Draft → InReview → Published → Superseded, never backwards. A Published or Superseded version is immutable. `Publish` needs an effective date and an approver different from the author. Services can be added only while Draft or InReview. |
| ServiceCalendar | id, code, name, runs on Mon..Sun, valid_from, valid_to | Code unique. Runs on a date when the weekday is set, the date is inside the valid range, and no `Removed` exception applies; or when an `Added` exception applies. |
| CalendarException | calendar_id, date, kind, reason | One per calendar and date. Kinds `Removed` and `Added`. |
| Service | id, version, route, calendar, train_number, direction | Train number is a label, unique within a version (REQ-LIVE-019). Direction is `Forward` or `Reverse` along the route. At least two stops. |
| ServiceStop | service, sequence, station, arrival_minutes, departure_minutes, default_platform, kind | See 5.5. |

Times are stored as **minutes after midnight of the service date**, so 23:50 is 1430 and a stop at 00:25 the next day is 1465. This is what REQ-LIVE-009 asks for (stop times may exceed 24:00), needs no time zone to store, and is explicit rather than computed from offsets.

### 5.2 Which version applies

For a service date D the applicable version is the version with status Published or Superseded and the greatest `effective_from` that is on or before D. A Superseded version still answers for dates before its successor takes over, so past dates keep the timetable that was true then. If none exists there is no timetable for D. This is the whole of REQ-LIVE-017: publishing a new version never changes the answer for dates before its effective date, so journeys already generated are untouched.

### 5.3 Contracts interface (for Journeys)

In `SmartRail.Modules.Network.Contracts`; implemented in Network.Infrastructure:

```
INetworkTimetable.GetServicesRunningOnAsync(DateOnly serviceDate, CancellationToken)
  -> IReadOnlyList<ScheduledService>

ScheduledService(Guid ServiceId, string TrainNumber, string RouteCode, IReadOnlyList<ScheduledStop> Stops)
ScheduledStop(int Sequence, string StationCode, int? ArrivalMinutes, int? DepartureMinutes,
              string? PlatformCode, string Kind)
```

This is the working view: passing points are included, because Journeys need them. Stations are referenced by code, never by internal id.

### 5.4 Public API

All anonymous and read-only. Same caching as SPEC-002 (`ETag`, `max-age=300`). Responses carry `timeZone`.

| Method and route | Success | Errors |
|---|---|---|
| `GET /api/network/stations/{code}/timetable?date=YYYY-MM-DD&kind=departures\|arrivals&lang=` | `200` `{ date, timeZone, kind, station, items: [{ serviceId, trainNumber, routeCode, scheduledTime, minutes, otherStationCode, otherStationName, platform }] }` sorted by time. For departures the other station is the destination, for arrivals the origin. `kind` defaults to departures. Passing points are never listed; services that only terminate here appear only in arrivals, and services that only start here only in departures | `400` bad date or kind, `404` unknown station, `404` `Network.NoTimetableForDate` |
| `GET /api/network/services/{serviceId}?date=YYYY-MM-DD&lang=` | `200` `{ date, timeZone, serviceId, trainNumber, routeCode, stops: [{ sequence, stationCode, stationName, scheduledArrival, scheduledDeparture, arrivalMinutes, departureMinutes, platform, kind }] }` public stops only, sequence renumbered | `400`, `404` unknown service, `404` service does not run on that date |

`date` defaults to today in the network time zone. Times are written `HH:mm`; after midnight they continue past 24 (`24:25`), the same convention as common timetable formats.

### 5.5 Rules for a service

1. Stops follow the route in order, forward or reverse. A service may skip stations (an express) but not reorder them.
2. At least two stops; a station appears once; sequence is 1..n with no gaps.
3. Times are 0 to 2879 minutes. Within a stop arrival ≤ departure; between stops departure ≤ next arrival.
4. The first stop has no arrival and the last has no departure.
5. A default platform must belong to the stop's own station, enforced by the database as in SPEC-002.
6. Kinds are `Stop`, `PickUpOnly`, `SetDownOnly`, `Passing`; the first and last stop are never `Passing`.

### 5.6 Data

Schema `network`, existing conventions (snake_case, checks, per-module migrations).

| Table | Columns | Database-enforced rules |
|---|---|---|
| `timetable_versions` | id, label, status, effective_from date null, author, approved_by null, published_at null, audit | check(status in (...)); check(status in ('Draft','InReview') or effective_from is not null); check(a Published or Superseded version has an approver different from the author); partial unique(effective_from) where status in ('Published','Superseded') |
| `service_calendars` | id, code, name, monday..sunday bool, valid_from date, valid_to date null | unique(code); check(valid_to is null or valid_to >= valid_from) |
| `calendar_exceptions` | calendar_id, date, kind, reason | pk(calendar_id, date); check(kind in ('Removed','Added')) |
| `services` | id, timetable_version_id, route_id, calendar_id, train_number, direction, service_category | unique(timetable_version_id, train_number); check(direction in ('Forward','Reverse')); check(service_category in ('Regional','Intercity','HighSpeed')), default Regional (REQ-TKT-008) |
| `service_stops` | service_id, sequence, station_id, arrival_minutes null, departure_minutes null, default_platform_id null, kind | pk(service_id, sequence); unique(service_id, station_id); composite fk(default_platform_id, station_id) to platforms; check(sequence >= 1); check both minutes between 0 and 2879; check(arrival <= departure when both set); check(kind in (...)) |

### 5.7 Seed

Insert-only, keyed on natural codes, fixed ids, flag-controlled (same rules as SPEC-002; flag `Network:Seed:ReferenceData`). Fictional demo (ASM-017), built from the four seeded routes:

| Calendar | Runs | Services |
|---|---|---|
| `WEEKDAYS` | Mon to Fri | Every route, both directions, four departures (06:30, 08:00, 12:00, 17:30): 32 services. One `Removed` exception on a demo holiday date |
| `DAILY` | every day | `MAIN` both directions at 20:00, and `HILL` forward at 23:50 so it ends after midnight |
| `WEEKEND` | Sat and Sun | `COAST` both directions at 10:00 |

About 37 services in one version, `Published`, label `Demo timetable`, effective from 2026-01-01, authored by `seed` and approved by `seed-approver`. Stop times are computed once at seed time from the route's running and dwell minutes (the reverse direction uses the same legs in reverse) and stored explicitly.

Network time zone: `Network:TimeZone`, an IANA id, validated at startup. Drafted default `Asia/Colombo` as a demo setting.

### 5.8 Business rules (summary)

1. A date resolves to exactly one version or to none (5.2).
2. The public view never shows passing points and renumbers the sequence.
3. A service appears on its own service date even when its stops pass midnight.
4. Inactive routes and stations are never shown.
5. Reads never expose internal ids except the service id, which is the public handle for a service.

### 5.9 Security and permissions

Network endpoints are explicitly anonymous; the allow-list test is updated for the two new routes. No personal data. Nothing is written through the API.

### 5.10 Observability

Existing traces and metrics. The seeder logs counts. A date with no timetable is logged at information level, not as an error.

## 6. Acceptance criteria

- [x] **AC-1** Given the seeded timetable, when I request a station's timetable for a weekday, then departures are sorted by time with train number, destination and platform, and arrivals with train number, origin and platform; a service that only ends there is only in arrivals and one that only starts there only in departures.
- [x] **AC-2** A weekday service appears on a Wednesday and not on a Saturday.
- [x] **AC-3** A `Removed` exception removes the services on that date; an `Added` exception adds a service on a date it does not normally run.
- [x] **AC-4** The 23:50 service shows times past 24:00 (for example `24:25`) and is listed under its own service date.
- [x] **AC-5** With two published versions, a date before the newer effective date uses the older version and a date on or after it uses the newer one.
- [x] **AC-6** A date with no applicable version returns `404` with code `Network.NoTimetableForDate`.
- [x] **AC-7** Service detail returns the public stops in order with scheduled arrival and departure; passing points are absent and the sequence is renumbered; a service that does not run on the date returns `404`.
- [x] **AC-8** An invalid date returns `400`; a missing date means today in the network time zone.
- [x] **AC-9** A service is rejected when its stops do not follow the route in order, a time goes backwards or is out of range, a station repeats, or it has fewer than two stops.
- [x] **AC-10** A Published version cannot receive services or be edited; the author cannot approve their own version; publishing without an effective date is rejected.
- [x] **AC-11** The database refuses a service stop whose default platform belongs to another station.
- [x] **AC-12** The seed creates one published version with about 37 services and the three calendars; running it again changes nothing; with the flag off nothing is seeded.
- [x] **AC-13** `INetworkTimetable` returns, for a date, the same services including passing points, with stations by code; the architecture tests show Journeys references only Network.Contracts.
- [x] **AC-14** The two new endpoints are anonymous and cacheable; every other endpoint still requires a token.

## 7. Test plan

| AC | Type | Location |
|---|---|---|
| AC-9, AC-10, calendar rules | Unit | `tests/SmartRail.Modules.Network.Tests/` |
| AC-5 version resolution, AC-3, AC-4 | Unit on the resolver plus integration with inserted versions | `tests/SmartRail.Api.IntegrationTests/Network/` |
| AC-1, AC-2, AC-6, AC-7, AC-8, AC-14 | Integration against Postgres (Testcontainers) | `tests/SmartRail.Api.IntegrationTests/Network/` |
| AC-11, AC-12 | Integration, direct inserts and seed runs | same |
| AC-13 | Integration of the interface plus the existing boundary tests | `ModuleBoundaryTests` |

Tests use a fixed clock (`TimeProvider`) wherever "today" matters.

## 8. Dependencies

- SPEC-002 (stations, platforms, routes, seeder, language and caching helpers).
- No new packages.

## 9. Risks

| Risk | Likelihood | Mitigation |
|---|---|---|
| Time zone on the host is misconfigured | Medium | Validate at startup; times stored without a zone; the zone is returned in every response |
| Version resolution is subtly wrong across a boundary date | Medium | Dedicated tests on the boundary day and the day before |
| Seeded times are unrealistic | Low | Rounded demo values, documented |
| Stored times drift from route legs after a route edit | Low | Services own their times; a route edit never rewrites them |

## 10. Design decisions

| # | Decision | Why |
|---|---|---|
| D1 | Store explicit minutes after midnight per stop, computed only at seed time | REQ-LIVE-009 needs times past 24:00 and no time zone in storage; explicit data survives later edits and works for expresses |
| D2 | One calendar shared by many services, with exceptions on the calendar | A holiday is one fact, not one per service; matches REQ-LIVE-008 acceptance (a holiday produces no journey) |
| D3 | Version resolved by greatest `effective_from` ≤ date, Superseded still answers past dates | Past dates must keep the timetable that was true (REQ-LIVE-017, EDG-001) |
| D4 | Version invariants (immutability, approver ≠ author) in the domain now, no workflow yet | They are Must requirements that are cheap now and costly to retrofit; the workflow stays deferred |
| D5 | Journeys read the timetable through a Contracts interface, not HTTP and not Network tables | Module boundary rule; one process today, but the seam is honest |
| D6 | Public API exposes the service id and date, not the train number | Train number is a label (REQ-LIVE-019) and is reused across days and versions |
| D7 | The station timetable serves both departures and arrivals from one query | The concourse display needs both (REQ-DSP-002); mirroring the same query costs little now and avoids a second endpoint later |
| D8 | Direction stays internal | The UI shows "to <destination>", never a compass direction |

## 11. Open questions

None. Answered on 2026-10-16 with the drafts below.

- **Q1.** Is `Asia/Colombo` an acceptable demo default for the network time zone, given ASM-017 says "single time zone" without naming one? (Configurable either way.)
- **Q2.** Keep the version rules (immutability, author ≠ approver) in the domain now, with unit tests but no API or screen (D4), or cut them to save about half a day?
- **Q3.** Is the seed size right: about 37 services across the 4 routes, 3 calendars and one demo holiday?

Drafted answers: Q1 yes, Q2 keep, Q3 yes. (The earlier question about arrivals is settled: they are in this spec, D7.)

## 12. Tasks

- [x] Domain: TimetableVersion, ServiceCalendar, CalendarException, Service, ServiceStop, rules and errors
- [x] Application: version resolver, services-on-date query, station timetable and service detail queries
- [x] Contracts: `INetworkTimetable` and its records
- [x] Infrastructure: mappings, migration, readers, `INetworkTimetable` implementation, seeder, time zone setting
- [x] API: two endpoints, date parsing, caching
- [x] Tests for every AC; update the anonymous allow-list
- [x] README: endpoints and settings

## 13. Review record

| Date | Reviewer | Decision | Notes |
|---|---|---|---|

## 14. Verification

| AC | Result | Evidence |
|---|---|---|
| AC-1 | Pass | `TimetableApiTests.A_station_lists_departures_and_arrivals_sorted_by_time`; `TimetableQueryTests` |
| AC-2 | Pass | `TimetableApiTests.Weekday_services_run_on_a_wednesday_and_not_on_a_saturday` |
| AC-3 | Pass | `TimetableApiTests.A_holiday_removes_services_and_a_special_day_adds_them`; `ServiceCalendarTests` |
| AC-4 | Pass | `TimetableApiTests.A_late_service_has_times_past_24_00_and_stays_on_its_service_date` |
| AC-5 | Pass | `TimetableVersionResolutionTests`; `VersionResolverTests` |
| AC-6 | Pass | `TimetableApiTests.A_date_with_no_timetable_is_not_found` |
| AC-7 | Pass | `TimetableApiTests.Service_detail_shows_public_stops_in_order_and_404_when_it_does_not_run` |
| AC-8 | Pass | `TimetableApiTests.An_invalid_date_or_kind_is_a_bad_request`; `TimetableDefaultDateTests` (fixed clock) |
| AC-9 | Pass | `ServiceTests` (route order, reverse, express, times, midnight, passing endpoints) |
| AC-10 | Pass | `TimetableVersionTests` (state order, author cannot approve, published is immutable) |
| AC-11 | Pass | `TimetableConstraintTests.Database_refuses_a_service_stop_with_a_platform_from_another_station` |
| AC-12 | Pass | `TimetableSeedTests`, `TimetableSeedOffTests` (1 version, 37 services, 3 calendars, 2 exceptions; repeat changes nothing) |
| AC-13 | Pass | `TimetableApiTests.Journeys_can_read_the_working_timetable_through_the_contract`; `ModuleBoundaryTests` |
| AC-14 | Pass | `TimetableApiTests.The_new_endpoints_are_anonymous_and_cacheable`; `SecureByDefaultTests` allow-list updated |

Run on 2026-10-16: build with `-warnaserror` clean; 61 unit, 25 architecture and 51 API tests pass (Postgres 16 in Docker). Manual run of the real API on a fresh database: seed added 3 calendars and 37 services; Central departures on Wednesday 14 October listed 14 trains; the holiday (23 Oct) left only the two daily services; the 23:50 HILL service ended at `24:24`; a date with no timetable returned `404 Network.NoTimetableForDate`; a bad date returned `400`.

Notes: `InvariantGlobalization` is now false because IANA time zone ids (for example `Asia/Colombo`) need ICU; the runtime image and CI have it.

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
| 2026-10-16 | Approved by the owner; arrivals added; data design aligned with `database-schema.sql` | Malindu |
