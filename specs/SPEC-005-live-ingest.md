# SPEC-005 Live feed ingest, delay and confidence

| Field | Value |
|---|---|
| Status | Verifying |
| Owner | Malindu |
| Created | 2026-10-16 |
| Approved | 2026-10-16, Malindu (on the instruction to build; drafts accepted for Q1 to Q9) |
| Branch | `feature/live-ingest` |
| Pull requests | — |
| Modules | Journeys (owner), Identity (feed source authentication, one Contracts interface), BuildingBlocks (metrics helper if needed) |
| Apps | API (the simulator and the real feed are its clients, built in SPEC-006) |
| Changes | API contract, data model, events, permissions (feed key scheme), observability |

**Pre-spec evaluation:** not trivial (a new authenticated write API, state changes on journeys, new tables). It is the largest backend spec in the register, so it is deliberately limited to the **train detection feed** (stop events and positions). Driver events (SPEC-011), controller actions (SPEC-010), realtime push (SPEC-007) and the simulator (SPEC-006) are separate. It depends on SPEC-004 (journeys, stops, assignments) and the agreed schema in `docs/02-architecture/database-schema.sql`. Deliverable in about four days; if the review thinks that is too long, the split point is "stop events and delay" (5a) versus "positions, staleness and plausibility" (5b).

## 1. Summary

Let the **train detection feed** tell the platform what trains are doing. An authenticated ingestion API accepts stop events (arrived, departed, passed) and position updates. Each message is validated (duplicates, out-of-order, implausible), recorded in the journey's event history with its source, and applied to the journey: lifecycle moves from Ready to Running to Completed, stops become Arrived and Departed, **delay** and **punctuality band** are computed, downstream **ETAs** are recomputed, and every estimate carries a **source and confidence**. When the feed goes quiet, the journey becomes **Stale** after 60 seconds and **Unknown** after 300 seconds, and says so. The public journey and board responses show all of this. Where two sources disagree, the documented **source precedence** decides and the loser is kept.

## 2. Motivation

- **This is the product's reason to exist** (MVP "Done when" 2, 3 and 6): a delay reaches the screens, the scheduled time does not change, and when the feed stops the screens say so instead of guessing.
- **Honesty rules are decided here.** Never show a departure earlier than scheduled (REQ-LIVE-012), never extrapolate a train that may have stopped (REQ-LIVE-004), never publish a ghost (REQ-LIVE-015), never let a bad ping corrupt every downstream ETA (REQ-LIVE-005).
- **Everything live depends on it:** the simulator (SPEC-006), realtime push (SPEC-007), the passenger timeline (SPEC-009), the controller board (SPEC-010) and the driver app's reconciliation (SPEC-011).

## 3. Traceability

| Kind | IDs |
|---|---|
| Requirements | REQ-LIVE-002 (punctuality separate from lifecycle), REQ-LIVE-003 (source and confidence on every estimate), REQ-LIVE-004 (Stale at 60 s, Unknown at 300 s), REQ-LIVE-005 (telemetry validation and metrics), REQ-LIVE-006 (source precedence, loser kept), REQ-LIVE-007 (ETA recomputation within 5 s p95), REQ-LIVE-009 (service date on boards while running), REQ-LIVE-010 (telemetry reaches a journey through its current assignment), REQ-LIVE-012 (early running rule), REQ-LIVE-015 (ghost and phantom, internal flag only), REQ-LIVE-019 (journey id is the identity), REQ-SEC-004 (feed source keys), REQ-LIVE-001 (Ready to Running to Completed transitions) |
| Assumptions | ASM-019 (stale at 60 s), ASM-020 (Unknown at 300 s), ASM-021 (punctuality bands), ASM-008 (the feed is train detection, not phone GPS), ASM-032 (offset propagation and dwell model) |
| State models | `04-domain/state-models.md` sections 1a (lifecycle), 1b (punctuality), 1c (confidence), 2 (journey stop) |
| Source precedence | `04-domain/source-precedence.md` rows for actual times, position, and the worked cases |
| Edge cases | EDG-031 (blackout), EDG-051 (duplicate), EDG-052 (out of order), EDG-053 (implausible jump), EDG-054 (phantom), EDG-055 (ghost), EDG-056 (forged source), EDG-002 (midnight crossing), EDG-033 (vehicle substitution) |
| Simulator design | `docs/02-architecture/simulator-design.md` (what it sends, the faults it injects) |
| Schema | `docs/02-architecture/database-schema.sql`: `journeys.journey_events`, `journeys.telemetry_readings`, `identity.feed_sources`, delay and confidence columns of `journeys.journeys` and `journey_stops` |
| Design | Passenger app: 07 Departures, 08 Journey (staleness and "as of" labels); Ops console: live board |

## 4. Scope

**In scope**
- A feed source credential scheme: `identity.feed_sources` with a hashed key and a precedence rank; an Identity Contracts interface to authenticate a source; unknown or inactive sources are rejected with 401 and counted.
- Two authenticated ingestion endpoints (batch, per-item results): stop events and positions.
- Validation: duplicates, out-of-order, implausible, unknown journey, journey not in a state that accepts events, stop that does not exist.
- The event history (`journey_events`) and raw telemetry (`telemetry_readings`) tables and migration, with the source id on every row.
- Applying accepted events to the journey: Ready to Running to Completed, stop states, current stop, running sub-state, `last_accepted_update_at`.
- Delay (signed minutes), punctuality band, per-stop estimates with source, ETA propagation with a per-stop dwell model, early running rule.
- Journey confidence (Scheduled, Live, Confirmed, Unknown) and the **Stale** marker, computed at read time from the clock.
- Source precedence between sources of different rank, loser kept as Superseded.
- Telemetry resolution through the current vehicle assignment, ghost and phantom handling (internal only).
- Public response changes: journey and board responses gain delay, band, estimates, source, confidence, age and the stale flag. No field that carries an estimate can be null for source or confidence.
- Domain events through the outbox for departed, completed and delay changed. The integration events that other modules consume are defined in SPEC-007, when there is a consumer.
- Metrics: accepted, rejected (by reason), unauthorized, ghost.

**Out of scope** (and the spec that builds it)

| Item | Why / where |
|---|---|
| Driver stop events, delay reasons, offline queue and replay, driver-versus-feed correction flow (REQ-DRV-003 to 011) | SPEC-011. The precedence rule is built here and tested with two feed sources of different rank; the driver becomes a third source there |
| Cancel, terminate short, interrupt, resume, platform change, reinstate endpoints and screens (REQ-OPS-004) | SPEC-010. The domain methods exist since SPEC-004 |
| Realtime push, throttling of pushes, live board contract | SPEC-007 |
| The simulator and its scenarios, the forged-source and fault injection clients | SPEC-006 (this spec is tested with test clients) |
| Unassigned vehicle and driver alerts (REQ-LIVE-011), vehicle failure cascade (REQ-LIVE-018) | SPEC-012 |
| Ghost and phantom lists on a screen (REQ-LIVE-015 "flagged internally") | SPEC-010. Here: structured log and metric only (Q7) |
| Purge of telemetry after 7 days (REQ-DATA-003) | Table is built with the purge index; the job is a separate small task (Q8) |
| Real distances between stations | The demo network has none. Plausibility uses section order and a speed ceiling (Q3) |

## 5. Detailed design

### 5.1 Source authentication (Identity)

- `identity.feed_sources` (code, name, key hash, precedence rank, active, last used) with a migration in the Identity module. A source sends its credentials in two headers: `X-Source-Code` and `X-Source-Key`. The key is a random 256-bit value; only its SHA-256 hash is stored; comparison is constant-time.
- Identity.Contracts gains `IFeedSources.AuthenticateAsync(code, key)` returning `FeedSourceRef(Id, Code, Rank)` or `null`. Journeys never reads the Identity tables.
- The Host adds an authentication scheme `FeedKey` and a policy `FeedSource`. The ingestion endpoints require that policy; they are not anonymous and are not reachable with a user token.
- Demo data: a source `SIM` (rank 1) and a second source `SIM-B` (rank 2, for precedence tests) are inserted only where missing, with keys from configuration (`Identity:FeedSources:Seed`), development and tests only. Keys are never logged.
- Unknown, inactive or missing credentials return `401` and increment the unauthorized counter (REQ-SEC-004).

### 5.2 Ingestion API

| Method and route | Who may call | Request | Success | Errors |
|---|---|---|---|---|
| `POST /api/ingest/stop-events` | Policy `FeedSource` | `{ "events": [ { "sequence": 1042, "journeyId": "…", "stopSequence": 1, "type": "departed", "occurredAt": "2026-10-14T02:00:03Z" } ] }` (1 to 100 items) | `200` `{ "results": [ { "sequence": 1042, "disposition": "Applied" } ] }` | `400` malformed batch or more than 100 items, `401` |
| `POST /api/ingest/positions` | Policy `FeedSource` | `{ "readings": [ { "sequence": 1043, "vehicleCode": "MAIN-F", "recordedAt": "…", "fromStation": "CEN", "toStation": "OAK", "progress": 0.42, "speedKmh": 74.5 } ] }` | `200` `{ "results": [ { "sequence": 1043, "disposition": "Applied", "journeyId": "…" } ] }` | `400`, `401` |

- `type` is `arrived`, `departed` or `passed`. `passed` is for a train that goes through a stop without stopping (Passing stops, or a skipped stop).
- A batch is never rejected as a whole because one item is bad. Each item has its own disposition: `Applied`, `Duplicate`, `Superseded`, `Rejected` (with a `reason`), `HeldForCorrection`, `Ghost`. HTTP `200` means the batch was understood.
- `sequence` is one counter per source, increasing across both endpoints. The server clock (`received_at`) decides ordering; `occurredAt` is stored and used for delay, never for ordering.
- `occurredAt` more than 5 seconds in the future is rejected as `implausible`.

### 5.3 Validation order (REQ-LIVE-005)

For each item, stop at the first failure and record why:

1. Source authenticated (batch level).
2. Journey exists (stop events) or vehicle code is known to the system (positions). An unknown journey id is `Rejected: unknown_journey` and is counted but not stored (the history table requires a journey).
3. `sequence` equal to one already accepted for this source: `Duplicate`, nothing changes. `sequence` lower than the highest accepted for this source: `Rejected: out_of_order`.
4. Plausibility: `occurredAt` not in the future; position section is the current section or the next one in route order (no jump of more than one section without a stop event); progress does not go backwards within a section by more than 0.02; `speedKmh` at most 160. Otherwise `Rejected: implausible`.
5. Journey state accepts events: Ready, Running (and Interrupted, for the feed to show it moving; the lifecycle change is SPEC-010/011). Planned (no vehicle or driver): `HeldForCorrection`. Completed, Cancelled, TerminatedShort, TerminatedEmergency: `Rejected: journey_ended`. Everything is kept in `journey_events` with its disposition and reason.
6. The stop exists on the journey.

### 5.4 Applying a stop event

| Event | Effect |
|---|---|
| `departed`, stop 1 | Ready becomes Running (sub-state `InSection`), stop 1 `Departed` with actual departure, first accepted update time set |
| `arrived`, stop n | Stop `Arrived` with actual arrival, sub-state `AtStop`, current stop n; at the final stop: journey becomes `Completed` |
| `departed`, stop n | Stop `Departed` with actual departure, sub-state `InSection` |
| `passed`, stop n | Stop `Departed` with actual times null and estimate source Confirmed (the feed saw it pass) |
| Any later stop confirmed while earlier stops are still Pending | The earlier Pending stops are resolved as `Departed` with no actual time, recorded as an inferred event (state-models.md section 2), and delay is taken from the newest confirmed time |

Delay is the newest confirmed time minus its scheduled time, in whole minutes, rounded to the nearest minute (Q5). Arrivals compare with scheduled arrival, departures with scheduled departure. The band follows state-models.md: Early 2 or more minutes ahead, OnTime under 2 minutes late, MinorDelay 2 to 9, MajorDelay 10 or more. Lifecycle is not touched by delay.

### 5.5 ETA recomputation (REQ-LIVE-007)

For every later stop, in sequence: the estimated arrival is the scheduled arrival plus the current delay, and the estimated departure is the later of the scheduled departure and the estimated arrival plus the minimum dwell (1 minute). A stop can therefore recover at most `scheduled dwell − minimum dwell` minutes, and a departure estimate is **never earlier than scheduled** (REQ-LIVE-012). Arrivals may be earlier than scheduled (early running). Estimates carry source `Live`; stops with actual times carry `Confirmed`; stops with no live basis stay `Scheduled`. Scheduled times are never written.

### 5.6 Confidence and staleness (REQ-LIVE-003, REQ-LIVE-004)

Journey confidence, computed on write and again at read time from `last_accepted_update_at` and the clock:

| Situation | Confidence | Delay shown | Extra |
|---|---|---|---|
| Planned or Ready, no feed update yet | Scheduled | Hidden | |
| Running, last accepted update under 60 s ago | Live | Shown | |
| Running, 60 to 300 s ago | Live | Shown | `isStale: true`, `ageSeconds`, "as of" time |
| Running, 300 s or more ago | Unknown | Hidden | Estimates revert to scheduled times; clients say live data is unavailable |
| Completed | Confirmed | Final delay | |

No background job is needed for Stale and Unknown: they are derived from the clock when a response is built. A new accepted update restores Live. Thresholds are configuration (`Journeys:Live:StaleAfterSeconds` 60, `UnknownAfterSeconds` 300) and use the injectable `TimeProvider`.

### 5.7 Source precedence (REQ-LIVE-006)

- Facts that can conflict: the actual time of a stop event (journey, stop, type). The key is stored on the event.
- The source with the **lowest rank number** wins. A later message from a source of **worse** rank for a fact that already has a winner is stored as `Superseded` (kept in history, never discarded). A message from a **better** rank replaces the current winner: the old event becomes `Superseded` and points to the new one, and delay and ETAs are recomputed.
- Same rank, same fact, already recorded: `Rejected: already_recorded`; the first one stands. (Changing a recorded time is a controller correction, SPEC-010.)
- Position updates are not subject to precedence; the best-ranked fresh source for the vehicle is used for the section.

### 5.8 Positions, ghosts and phantoms (REQ-LIVE-010, REQ-LIVE-015)

- A reading resolves to a journey through the **current vehicle assignment** at its `recordedAt`. After a mid-journey substitution (SPEC-004 assignments) the new vehicle's readings reach the same journey and the old vehicle's later readings do not.
- A reading with no matching journey (a phantom) is stored with `resolved_journey_id` null, answered `Ghost`, counted, logged, and **never appears on any public endpoint**.
- A Running journey with no feed update for the Unknown threshold is the "ghost journey" case: it turns Unknown to the public and is logged and counted for operations (Q7).
- A resolved, accepted position refreshes `last_accepted_update_at`, and sets sub-state `InSection`. The section itself is kept in `telemetry_readings` for the plausibility check and is not exposed publicly (the journey shows its stops and states, not coordinates).

### 5.9 Public API changes (SPEC-004 endpoints, same routes)

- `GET /api/journeys/{id}` and board rows gain: `delayMinutes`, `punctuality`, `confidence`, `isStale`, `ageSeconds`, `asOfUtc`, and per stop `estimatedArrivalUtc`, `estimatedDepartureUtc`, `actualArrivalUtc`, `actualDepartureUtc`, `estimateSource`.
- `confidence` and `estimateSource` are never null. Delay is null when hidden (Unknown, Interrupted). A departure estimate is never earlier than the scheduled departure.
- The board orders by the effective departure (estimated, never before scheduled). A delayed Running train stays on the board until it has departed the station.
- Caching stays 5 seconds with ETag.

### 5.10 Events (outbox)

The journey raises domain events, which the interceptor writes to the module's outbox in the same transaction as the change:

| Event | Kind | Raised when |
|---|---|---|
| `JourneyDepartedDomainEvent` | domain | First departure makes a journey Running |
| `JourneyCompletedDomainEvent` | domain | Final arrival |
| `JourneyDelayChangedDomainEvent` | domain | The delay changes (not on every message) |

SPEC-007 translates them into integration events in `Journeys.Contracts` when realtime push gives them a consumer.

### 5.11 Data

- `journeys.journey_events` and `journeys.telemetry_readings` exactly as in the agreed schema, via an EF migration in the Journeys module (partial unique indexes on `(source_id, device_sequence)` and `(source_id, sequence)`; append-only trigger on events using the existing `forbid_update_delete` pattern).
- `identity.feed_sources` via an Identity migration.
- `journey_events` gains a `source_rank` column (not in the first schema draft) so the history records which rank a source had when it spoke; `docs/02-architecture/database-schema.sql` is updated. The event type list grows with the specs that use it (`Passed` is added here).
- Journeys already has the delay, band, confidence and `last_accepted_update_at` columns.

### 5.12 Rules, security, observability

- Optimistic concurrency on `journeys.version`: two messages for one journey in parallel are both applied (the loser retries up to 3 times).
- Raw payloads are not logged; keys are never logged or returned.
- Metrics (OpenTelemetry meter `SmartRail.Ingest`): `ingest.accepted`, `ingest.rejected` (tag `reason`), `ingest.unauthorized`, `ingest.ghost`, `ingest.duplicate`; apply-latency histogram.
- Rate limiting of the ingestion endpoints belongs to SPEC-017 (REQ-SEC-002); the batch limit of 100 is enforced here.

## 6. Acceptance criteria

| AC | Given / When / Then | Traces to |
|---|---|---|
| AC-1 | Given no credentials, an unknown source code, a wrong key or an inactive source, when a batch is posted, then the response is `401`, nothing is stored, and the unauthorized counter increases. A known source is accepted and its id is stored on every event. | REQ-SEC-004, EDG-056 |
| AC-2 | Given a Ready journey, when the feed reports `departed` at stop 1, then the journey is Running with sub-state InSection, stop 1 is Departed with the actual time, and the event is stored as Applied with the source id. | REQ-LIVE-001, 010 |
| AC-3 | Given a Running journey, when stop 1 departs 8 minutes after schedule, then delay is +8, band MinorDelay, lifecycle is still Running, downstream estimates equal scheduled plus the delay (less recovered dwell) with source Live, and no scheduled time has changed. | REQ-LIVE-002, 007, 017 |
| AC-4 | Given a train that arrives 4 minutes early at a stop, then its arrival shows early (delay −4, band Early) and its departure estimate equals the scheduled departure, never earlier. | REQ-LIVE-012 |
| AC-5 | Given an accepted message, when the same sequence is sent again, then the item is `Duplicate`, no journey, stop or history row changes, and the duplicate counter increases. | REQ-LIVE-005, EDG-051 |
| AC-6 | Given the highest accepted sequence is 50, when a message with sequence 40 arrives, then it is `Rejected: out_of_order` and recorded. | REQ-LIVE-005, EDG-052 |
| AC-7 | Given a vehicle in the CEN to OAK section, when a reading places it two sections ahead within seconds, or reports a speed over the ceiling, or an `occurredAt` in the future, then it is `Rejected: implausible` with the reason recorded and the rejected counter increases. | REQ-LIVE-005, EDG-053 |
| AC-8 | Given source SIM (rank 1) and SIM-B (rank 2) report the arrival at stop 3 with different times, when SIM-B arrives first and SIM second, then SIM's time becomes the authoritative one, SIM-B's event is `Superseded` and kept, and delay is recomputed. When SIM-B arrives second, it is stored as `Superseded` and the state does not change. Two events of the same rank: the first stands. | REQ-LIVE-006 |
| AC-9 | Given a Running journey, when the final stop `arrived` is accepted, then the journey is Completed, confidence is Confirmed, and the final delay stays. | REQ-LIVE-001, 003 |
| AC-10 | Given stops 2 and 3 are still Pending, when stop 4 is confirmed, then stops 2 and 3 become Departed with no actual time (recorded as inferred), and delay follows stop 4. | state-models §2 |
| AC-11 | Given a Planned journey (no vehicle or driver), when a stop event arrives, then it is `HeldForCorrection` and the journey state does not change. | REQ-LIVE-001 |
| AC-12 | Given a journey id that does not exist, then the item is `Rejected: unknown_journey` and counted. Given a Cancelled or Completed journey, then the item is `Rejected: journey_ended` and kept in history. | REQ-LIVE-005 |
| AC-13 | Given a Running journey with a fixed clock, when the last accepted update is 61 s old, then the journey is `isStale` with its age and still Live; at 301 s it is Unknown, delay is hidden, and every estimate shows the scheduled time (tolerance ±5 s in the simulator test of SPEC-006). | REQ-LIVE-004 |
| AC-14 | Given a journey that went Unknown, when a new update is accepted, then it is Live again and estimates are recomputed. | REQ-LIVE-004 |
| AC-15 | Given a vehicle assigned to a journey, when readings arrive, then they resolve to that journey and refresh its last update. After the vehicle is replaced at stop 4, the new vehicle's readings reach the same journey and the old vehicle's later readings are `Ghost`. | REQ-LIVE-010 |
| AC-16 | Given a reading for a vehicle with no journey, then it is stored as a ghost, answered `Ghost`, and no public endpoint returns it or reveals the vehicle. | REQ-LIVE-015, EDG-054 |
| AC-17 | Given any journey or board response, then `confidence` and every `estimateSource` are present and not null; delay is null only when hidden. | REQ-LIVE-003 |
| AC-18 | Given a 23:50 journey that is Running at 00:10 the next calendar day, then it is on the previous service date's board as Running, with its delay. | REQ-LIVE-009, EDG-002 |
| AC-19 | Given a delay that changes the band, then exactly one `JourneyDelayChangedEvent` is raised; given repeated messages with the same delay, none. Departure and completion each raise their event once. | REQ-LIVE-007 |
| AC-20 | Given two messages for the same journey processed in parallel, then both are applied and the final state equals applying them in sequence order. | REQ-OPS-010 (concurrency pattern) |
| AC-21 | Given a batch of more than 100 items, then `400`. Given a batch with one bad item, then the others are still processed and each has its own disposition. | REQ-LIVE-005 |
| AC-22 | Given any accepted event, then no scheduled time of the journey or its stops has changed. | REQ-LIVE-017 |
| AC-23 | Given an applied update, then ETA recomputation of the journey finishes within 5 seconds at p95 in the local performance check. | REQ-LIVE-007 |

## 7. Test plan

| Area | Kind | Where |
|---|---|---|
| Delay, band, rounding, early running, dwell and ETA propagation (AC-3, 4, 22) | Unit, pure functions with a fixed clock | `tests/SmartRail.Modules.Journeys.Tests/` |
| Staleness and Unknown boundaries (AC-13, 14), confidence table | Unit with a fixed `TimeProvider` | same |
| Validation order and plausibility rules (AC-5, 6, 7, 12) | Unit | same |
| Precedence between ranks (AC-8) | Unit plus one integration test | same and `tests/SmartRail.Api.IntegrationTests/Journeys/` |
| Ingestion through the real API on Postgres: auth, stop events, positions, batches, concurrency, ghost, public responses, midnight crossing, events (AC-1, 2, 9, 10, 11, 15 to 21) | Integration, real Postgres, feed headers | `tests/SmartRail.Api.IntegrationTests/Journeys/` |
| Boundary: Journeys reads Identity only through Contracts | Architecture | existing `ModuleBoundaryTests` |
| AC-23 latency | A small local check (not part of CI) with the numbers recorded in verification | manual |

## 8. Dependencies

- SPEC-004 (journeys, stops, assignments, the clock and time zone contracts).
- Identity module persistence for `feed_sources` (the module has an Infrastructure project; this spec adds its first table if none exists yet).
- Next: SPEC-006 sends these messages; SPEC-007 pushes the events; SPEC-009 shows the result.

## 9. Risks

| Risk | Mitigation |
|---|---|
| Too big for one spec | Split point named in the header (5a stop events and delay, 5b positions and staleness) |
| A source restarts and its sequence counter resets, so its messages are rejected as out of order | Document the rule for clients: use a monotonic counter that survives restarts (the simulator will use a persisted counter or time-based value); an administrator can rotate the source |
| Plausibility without distances is weaker than the requirement example (40 km in 10 s) | Section order plus speed ceiling catch the cases the demo network can express; distances can be added to Network later without changing the API |
| Read-time Stale and Unknown could disagree with a pushed value | Both use the same function and clock; SPEC-007 pushes a change when the threshold is crossed using a scheduled check |
| Ordering by server receive time with batching | Sequence, not time, decides order inside a source; only the highest accepted sequence matters |
| Dwell model is a simplification | It is explainable and unit-tested (REQ-LIVE-007 rationale); refinement does not change the API |

## 10. Design decisions

| # | Decision | Why |
|---|---|---|
| D1 | The feed is a separate authentication scheme (source key headers), not a user token | A feed is a machine, not a person; its key can be rotated and revoked without touching users |
| D2 | Batch endpoints with a disposition per item | Real feeds batch; one bad reading must not hide good ones, and every rejection stays visible |
| D3 | Sequence is per source and decides order; occurredAt decides delay | Matches the agreed schema: server clock is the authority, device time is never trusted for ordering |
| D4 | Stale and Unknown are derived at read time | No background job that can fall behind; the same clock rule everywhere; testable with a fixed clock |
| D5 | The loser of a precedence conflict is kept as Superseded | REQ-LIVE-006: the disagreement stays visible |
| D6 | Unknown journey ids are counted, not stored | The history table has a journey foreign key; storing garbage ids would make it a dumping ground |
| D7 | Events for a Planned journey are held, not applied | The state machine says Running needs Ready; silently skipping a state would hide a missing assignment |
| D8 | Delay and bands never change the lifecycle | REQ-LIVE-002 |

## 11. Open questions

- **Q1.** Is the "split into 5a and 5b" fallback acceptable if the review finds four days too long? Draft: yes.
- **Q2.** When a later stop is confirmed, are earlier Pending stops marked Departed with no time (inferred)? Draft: yes.
- **Q3.** Plausibility without distances: section order, progress and a 160 km/h ceiling. Should station distances be added to the Network seed so the 40 km example from REQ-LIVE-005 can be tested literally? Draft: no, not now.
- **Q4.** Dwell model: minimum dwell 1 minute, a stop recovers at most `scheduled dwell − 1`. Draft: yes.
- **Q5.** Delay is rounded to the nearest minute (half rounds away from zero). Draft: yes.
- **Q6.** Feed credentials as two headers and a SHA-256 key hash. Draft: yes.
- **Q7.** Ghost and phantom: log and metric only now, the list for controllers in SPEC-010. Draft: yes.
- **Q8.** The 7-day telemetry purge is a separate small task after this spec. Draft: yes.
- **Q9.** Events for a Planned journey are held (`HeldForCorrection`) until a controller or the assignments make it Ready. Draft: yes.

## 12. Tasks

- [x] Identity: `feed_sources` table, migration, `IFeedSources` contract, demo sources `SIM` and `SIM-B`
- [x] Host: `FeedKey` authentication scheme and `FeedSource` policy; update the anonymous allow-list test (none added) and the role tests
- [x] Domain: stop event application, delay and band, ETA propagation, confidence, precedence rules
- [x] Application: ingest stop events command, ingest positions command, validation chain, journey and board query changes
- [x] Infrastructure: `journey_events` and `telemetry_readings` mappings and migration, vehicle assignment lookup, outbox events
- [x] API: the two ingestion endpoints, response changes
- [x] Metrics: ingest meter and counters
- [x] Tests for every AC
- [x] README: ingestion endpoints, demo source keys (development only), settings

## 13. Review record

| Date | Reviewer | Decision | Notes |
|---|---|---|---|
| 2026-10-16 | Malindu | Approved | Instruction to start building; drafts for Q1 to Q9 accepted |

## 14. Verification

| AC | Result | Evidence |
|---|---|---|
| AC-1 | Pass | `LiveIngestTests.Only_a_known_active_source_with_the_right_key_is_let_in`, `Rejections_and_unauthorized_attempts_are_counted` |
| AC-2 | Pass | `LiveJourneyTests.The_first_departure_makes_a_ready_journey_running`; `LiveIngestTests.A_late_departure_makes_the_journey_running_...` |
| AC-3 | Pass | `LiveJourneyTests.A_late_departure_gives_a_delay_a_band_and_downstream_estimates_without_touching_the_schedule`; the API test above |
| AC-4 | Pass | `LiveJourneyTests.Early_running_shows_an_early_arrival_but_never_an_earlier_departure` |
| AC-5 | Pass | `LiveIngestTests.A_duplicate_changes_nothing_and_an_old_sequence_is_rejected_as_out_of_order` |
| AC-6 | Pass | same test |
| AC-7 | Pass | `LiveIngestTests.An_event_from_the_future_...`, `Positions_reach_the_journey_through_the_vehicle_assignment_and_survive_a_substitution` (going back, jumping a section, speed over the ceiling, future time) |
| AC-8 | Pass | `LiveIngestTests.The_better_ranked_source_wins_and_the_loser_is_kept` |
| AC-9 | Pass | `LiveJourneyTests.Arriving_at_the_last_stop_completes_...`; `LiveIngestTests.A_journey_that_reaches_its_last_stop_completes_and_leaves_the_board` |
| AC-10 | Pass | `LiveJourneyTests.A_later_confirmed_stop_resolves_earlier_pending_stops_...`; the API test above |
| AC-11 | Pass | `LiveIngestTests.A_journey_without_vehicle_and_driver_holds_the_event_and_a_cancelled_one_refuses_it` |
| AC-12 | Pass | same test, and `An_event_from_the_future_and_a_batch_with_one_bad_item_are_handled_item_by_item` (unknown journey) |
| AC-13 | Pass | `LiveJourneyTests.A_running_journey_goes_stale_after_60_seconds_and_unknown_after_300` (boundaries 59/60/299/300); `LiveIngestTests.Live_data_goes_stale_...` (API and board). The plus or minus 5 s check with the simulator belongs to SPEC-006 |
| AC-14 | Pass | `LiveIngestTests.Live_data_goes_stale_...` (new update restores Live) |
| AC-15 | Pass | `LiveIngestTests.Positions_reach_the_journey_through_the_vehicle_assignment_and_survive_a_substitution` |
| AC-16 | Pass | same test (the phantom is stored with no journey and absent from the board) |
| AC-17 | Pass | `LiveIngestTests.A_late_departure_makes_...` checks every stop has a source; confidence is never null in `LiveStatus` (`Confidence_for_journeys_that_are_not_running_...`) |
| AC-18 | Pass | `PostMidnightLiveTests.A_running_journey_of_the_previous_service_date_shows_as_running_on_the_board_after_midnight` |
| AC-19 | Pass | `LiveIngestTests.A_departure_a_delay_change_and_a_completion_are_each_raised_once` (outbox rows); `LiveJourneyTests.A_delay_change_is_raised_once_...` |
| AC-20 | Pass | `LiveIngestTests.Two_sources_writing_to_the_same_journey_at_once_both_get_applied` |
| AC-21 | Pass | `LiveIngestTests.A_batch_of_more_than_100_is_refused`, `An_event_from_the_future_and_a_batch_with_one_bad_item_...` |
| AC-22 | Pass | `LiveJourneyTests.A_late_departure_gives_...` and the API test (scheduled times compared before and after) |
| AC-23 | Not measured | No latency measurement has been taken; each message is one short transaction. To be recorded before this spec is marked Done |

Build: `dotnet build SmartRail.slnx -c Release -warnaserror` clean. Tests: Journeys unit 103, Network unit 61, architecture 25, API integration 85, all passing. Not yet done: a manual run of the real app in Docker or Aspire.

## 15. Sync status

- [ ] Requirements or use cases updated if this spec changed them
- [ ] ADR created or updated
- [x] README or developer docs updated
- [ ] Traceability regenerated
- [x] Register in `specs/README.md` updated

## 16. Change log

| Date | Change | Approved by |
|---|---|---|
| 2026-10-16 | Drafted | — |
| 2026-10-16 | Approved, Q1 to Q9 drafts accepted | Malindu |
| 2026-10-16 | Built. Design changes while building: `source_rank` column on `journey_events`; domain events through the outbox, integration events left to SPEC-007; no public current-section field | — (to be confirmed at review) |
