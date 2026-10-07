# Journey simulator — design

## Why the simulator exists, and what it pretends to be

SmartRail has no access to a real railway's train-detection data. Something must tell the backend where trains
are. The tempting shortcut is a script that writes fake positions into the database. That is rejected, because it
would make every test and every demo prove a shortcut rather than the system (REQ-SIM-001).

The simulator is designed as a **stand-in for railway infrastructure**:

> It plays the **train detection feed** — the signalling-derived data a real operator would receive — not a GPS
> tracker on the driver's phone (ASM-008).

Consequences:

| Decision | Consequence |
|---|---|
| Publishes only through the public, authenticated **ingestion API** with its own source key | Validation, plausibility checks, precedence and realtime fan-out are all exercised exactly as in production (REQ-SIM-001, REQ-SEC-004) |
| Treated as one **feed source** among potentially many | A second source (e.g. a real feed later) needs no redesign |
| Independent of the driver app | Driver connectivity loss never blinds passengers — the defining property of CF-03 |
| Can also emit **driver-app events** through the driver API (optional per scenario) | E2E tests can script the driver side without a phone |

## What it produces

| Message | Through | Fields |
|---|---|---|
| Position update | Ingestion API | source id, journey id (via vehicle assignment), sequence, event time, location (between stop *n* and *n+1*, fraction), speed |
| Stop event | Ingestion API | source id, journey id, stop sequence, arrived / departed / passed, event time |
| Driver event *(optional)* | Driver API with a driver test token | duty id, stop event or delay reason, device time, sequence, idempotency key |
| Operator action *(optional)* | Ops API with a test controller token | cancel, platform change, interrupt, resume — so a whole cross-app flow can be scripted |

## Time

Everything in SmartRail reads time through an injectable clock (REQ-NFR-005). The simulator drives a shared
**scenario clock** that can run at 1× to 60× (REQ-SIM-002):

- In **tests**, the clock is advanced explicitly — a 30-minute journey runs in milliseconds and is deterministic.
- In the **demo**, 10–20× makes a journey visible in a couple of minutes without editing any threshold.
- Thresholds (60 s stale, 300 s Unknown) are measured in **scenario time**, so the same assertions hold at any speed.

## Determinism

A scenario is a seed plus a script. The same seed and script produce a byte-identical event stream
(REQ-SIM-002). Randomness (dwell variation, small running noise) comes only from the seeded generator.

## Scenario script format

```yaml
id: SIM-02
name: Delay 8 minutes between Riverside and Mill Lane
seed: 1042
speed: 20            # x real time (ignored in tests)
journeys:
  - service: "1042"
    date: today
    vehicle: S12
    driver: D-07
    run: normal
steps:
  - at: "08:14"
    journey: "1042"
    action: hold_in_section          # stop between stops, keep reporting
    duration: 8m
  - at: "08:20"
    journey: "1042"
    driver: delay_reason
    reason: signal
expect:
  - at: "08:22"
    journey: "1042"
    delta_minutes: 8
    band: MinorDelay
  - followers_push_count: { journey: "1042", max: 1, window: "08:17-08:30" }
```

## Fault injection (REQ-SIM-003)

| Fault | Effect | Exercises |
|---|---|---|
| `blackout` | Source stops sending for a journey for *n* minutes | Stale → Unknown (EDG-031) |
| `duplicate` | Re-sends the last *n* messages | Idempotency (EDG-051) |
| `reorder` | Delivers a batch out of order | Sequence rejection (EDG-052) |
| `jump` | Places the train an impossible distance away | Plausibility (EDG-053) |
| `skew` | Offsets driver device time | Clock handling (EDG-042) |
| `phantom` | Emits movement for a vehicle with no journey | Public filtering (EDG-054) |
| `ghost` | Schedules a journey but emits nothing | Ghost detection (EDG-055) |
| `driver_offline` | Holds driver events for *n* minutes, then releases them in a burst | Offline replay (EDG-044, EDG-046) |
| `forged_source` | Sends with an unknown key | Feed authentication (EDG-056) |

## Scenario library (REQ-SIM-004)

| ID | Scenario | Faults / actions | Demonstrates | Edge cases | E2E |
|---|---|---|---|---|---|
| SIM-01 | On-time baseline | — | Normal running, ETA accuracy | — | baseline |
| SIM-02 | Held 8 min in section | hold, driver reason | Delay propagation, push throttling | EDG-062, 070, 113 | **E2E-1** |
| SIM-03 | Skip a stop | operator skip | Per-stop state, follower at skipped stop | EDG-034 | — |
| SIM-04 | Terminate short | operator terminate | Remaining stops cancelled | EDG-036 | — |
| SIM-05 | Feed blackout | blackout 6 min | Stale at 60 s, Unknown at 300 s | EDG-031 | E2E-5 support |
| SIM-06 | Breakdown in section, then resume | hold, operator interrupt / resume | No-estimate rendering; rotation cascade | EDG-016, 030, 075 | — |
| SIM-07 | Vehicle substitution | assignment change mid-journey | Identity continuity | EDG-033 | — |
| SIM-08 | Cancellation mid-journey | operator cancel | Fan-out, audit, catering event | EDG-073, 114 | **E2E-3** |
| SIM-09 | Driver offline 15 min | driver_offline | Queue, replay, precedence, message states | EDG-044 … 049 | **E2E-2** |
| SIM-10 | Feed and driver both dark | blackout + driver_offline | Unknown with no rescue | EDG-032 | — |
| SIM-11 | Bad telemetry | duplicate, reorder, jump | Validation and rejection counters | EDG-051 … 053, 057 | — |
| SIM-12 | Phantom and ghost | phantom, ghost | Public filtering, ops flags | EDG-054, 055 | — |
| SIM-13 | Midnight crossing | service 23:50 → 00:35 | Service date handling | EDG-002 | — |
| SIM-14 | Late platform change | operator platform change | Platform state, urgent push, station highlight | EDG-021 | — |
| SIM-15 | Section blocked | operator section disruption | Scope resolution to many journeys | EDG-038, 067, 088 | — |
| SIM-16 | Driver clock skew | skew +6 min | Confidence downgrade, ordering | EDG-042 | — |
| SIM-17 | Major incident | operator emergency termination | Templates, legal hold | EDG-037 | — |
| SIM-18 | Demo day | composite of 02, 09, 14, 08, 19, 20, 05 | The stakeholder demo narrative | — | demo |
| SIM-19 | Ticket purchase with payment faults | provider: duplicate callback, bad signature, abandoned payment; operator cancels mid-checkout | Idempotency, signature checks, double availability check, automatic refund | EDG-125, 126, 127, 128 | **E2E-4** |
| SIM-20 | Cancellation with tickets and refund | operator terminates short; provider refund fails once, then succeeds on retry | Affected only for the lost part; one refund; retry; inspector rejection | EDG-123, 124, 129, 130, 131, 142 | **E2E-4** |

## Fake payment provider

The same simulator package ships a **fake payment provider** that implements the payment port (REQ-PAY-001), so purchases and
refunds can be driven with no external account:

| Capability | Behaviour |
|---|---|
| Hosted checkout page | A minimal page with Pay and Cancel; the real one is a redirect to the provider |
| Callback | Signed confirmation to SmartRail, with a timestamp (REQ-PAY-003) |
| Faults | `duplicate` (send twice), `badsig`, `stale` (old timestamp), `abandon`, `delay` |
| Refund | Idempotent on SmartRail's key; faults `fail_once`, `fail_always` |

The PayHere sandbox adapter, if built, implements the same port and runs the same tests.

## Reset (REQ-SIM-005)

A `POST /test/scenario/reset` endpoint clears live state and reloads a scenario. It is registered **only** when the
environment is not Production; under production configuration the route does not exist (404).

## Placement in the solution

```
backend/src/SmartRail.Simulator/      .NET worker or CLI
  Scenarios/*.yaml                     the library above
  Clock/ScenarioClock.cs               shared, controllable time
  Feed/IngestionClient.cs              calls the public ingestion API with a source key
  Driver/DriverClient.cs               optional driver-side events
  Faults/                              the fault injectors
```

The simulator references **no** backend project except the published API contracts. If it compiles against the
domain or infrastructure layers, that is a design defect — it would be able to cheat.
