# State models

Every state machine in SmartRail, in one place. Code, tests, UI copy and the simulator all use these names.
Illegal transitions are rejected and logged, never silently accepted (REQ-LIVE-001).

## 1. Journey — three independent dimensions

A journey is **not** in a single "status". It has three independent dimensions, and the UI composes them.

### 1a. Lifecycle

```
                ┌──────────────┐
                │   Planned    │  generated from timetable (REQ-LIVE-008)
                └──────┬───────┘
                       │ vehicle and driver assigned
                ┌──────▼───────┐
        ┌───────│    Ready     │───────┐
        │       └──────┬───────┘       │
        │              │ departs origin│
        │       ┌──────▼───────┐       │
        │       │   Running    │◀──────┼──────── resumed
        │       │ (AtStop /    │       │              │
        │       │  InSection)  │───────┼─────▶ Interrupted ──▶ TerminatedShort
        │       └──┬────────┬──┘       │              │
        │          │        │          │              └──▶ Cancelled
        │  arrives │        │ ends     │
        │ terminus │        │ early    │
        │   ┌──────▼──┐  ┌──▼──────────┴─────┐
        │   │Completed│  │  TerminatedShort  │
        │   └─────────┘  └───────────────────┘
        │
        └──────▶ Cancelled  (from Planned or Ready)

   Any state except Completed ──▶ TerminatedEmergency   (`incident.declare` only, REQ-OPS-007)
   Cancelled ──▶ Planned        (reinstated, before scheduled departure, with reason)
```

| State | Meaning | Entered when | Public? | Can go to |
|---|---|---|---|---|
| **Planned** | Exists in the plan for this service date | Generated nightly or added manually | Yes | Ready, Cancelled |
| **Ready** | Vehicle and driver assigned | Both assignments present | Yes (shown as Planned) | Running, Cancelled |
| **Running** | Has departed origin, not yet finished | First departure recorded | Yes | Completed, TerminatedShort, Interrupted, TerminatedEmergency |
| **Interrupted** | Stopped and cannot currently continue | Breakdown, line blocked, incident | Yes — "Stopped, no estimate" | Running (resumed), TerminatedShort, Cancelled, TerminatedEmergency |
| **Completed** | Reached its final stop | Arrival at terminus | Briefly, then removed | — |
| **TerminatedShort** | Ended before its planned terminus | Controller decision | Yes, with the stop where it ended | — |
| **Cancelled** | Will not run, or will not run further | Controller decision with reason | Yes, retained on boards for 15 min (ASM-024) | Planned (reinstated) |
| **TerminatedEmergency** | Ended because of a serious incident | `incident.declare` permission only | Yes, approved template text only | — |

**Sub-state of Running:** `AtStop` (at a station, dwelling) or `InSection` (between two stations). The driver app
uses this to disable free-text input between stops (REQ-DRV-004).

### 1b. Punctuality

| Band | Rule (ASM-021) | Passenger wording |
|---|---|---|
| Early | 2 or more minutes ahead | "Early" (arrivals only; departures never shown early, REQ-LIVE-012) |
| OnTime | under 2 minutes late | "On time" |
| MinorDelay | 2 to 9 minutes late | "Delayed +n" |
| MajorDelay | 10 or more minutes late | "Delayed +n" with emphasis |

Punctuality is meaningless in `Interrupted` (no ETA exists) and `Unknown` confidence (we do not know). In those
cases the delta is hidden, not shown as zero.

### 1c. Data confidence

| Value | Meaning | Rendering |
|---|---|---|
| **Confirmed** | A person or the feed has confirmed this event happened | Plain time |
| **Live** | Estimate based on current feed data | Time with a "live" marker |
| **Scheduled** | No live basis; this is the timetable | "Scheduled" label; no live marker |
| **Unknown** | Running, but no accepted update for 300 s (ASM-020) | "No live data — showing timetable" |

Between 60 s and 300 s without an update, data is **stale**: still shown, with "as of hh:mm" (ASM-019).

### How the three compose — examples

| Lifecycle | Punctuality | Confidence | Passenger sees |
|---|---|---|---|
| Running | +8 MinorDelay | Live | "Delayed +8 · live" |
| Running | +8 MinorDelay | Live, 90 s old | "Delayed +8 · as of 14:31" |
| Running | (hidden) | Unknown | "No live data — showing timetable 14:25" |
| Interrupted | (hidden) | Live | "Stopped near Riverside · no estimate" |
| Cancelled | +12 at cancellation | Confirmed | "Cancelled" (+ reason) |
| Planned | +5 predicted | Live | "Expected 14:30 (+5)" — late inbound vehicle |

## 2. Journey stop

| State | Meaning |
|---|---|
| Pending | Not yet reached |
| Arrived | At the platform |
| Departed | Has left |
| Skipped | Will not stop (station closed, controller decision, running late) |
| Cancelled | The journey was cancelled or terminated before this stop |

A lost event never leaves a stop `Pending` forever: if a later stop is confirmed, an earlier unconfirmed stop
is resolved as passed with confidence `Unknown`, not left hanging (see EDG catalogue).

## 3. Platform (per stop, per journey)

| State | Meaning | Rendering |
|---|---|---|
| TBA | Not yet assigned | "Platform —" / "To be announced" |
| Assigned | Allocated | "Platform 3" |
| Changed | Re-allocated after first assignment | "Platform **5** (changed from 3)" |

Every change carries a timestamp and author (REQ-LIVE-014).

## 4. Disruption

```
Draft ──▶ Published ──▶ Updated ──▶ Resolved
  │           │            │
  │           └────────────┴──▶ Retracted   (it was wrong — distinct from resolved)
  │           └────────────┴──▶ Expired     (review-by passed, needs a decision)
  └──▶ (planned) Scheduled ──▶ Published at start time
```

| State | Meaning |
|---|---|
| Draft | Being written; network-wide drafts wait here for a second approver (REQ-OPS-005) |
| Scheduled | Planned works with a future start (REQ-OPS-016) |
| Published | Visible on every affected surface |
| Updated | Published with newer content; history kept |
| Resolved | The disruption is over |
| Retracted | It should never have been published; a correction is sent if a push went out |
| Expired | Its review-by time passed without action; flagged to operations (REQ-OPS-006) |

## 5. Device (display or terminal)

| State | Meaning |
|---|---|
| Pending | Provisioned, never connected |
| Online | Heartbeat within 30 s |
| Stale | Missed heartbeats, under 90 s |
| Offline | No heartbeat for 90 s (ASM-029) |
| Revoked | Token revoked; all requests rejected |

## 6. Crew duty

| State | Meaning |
|---|---|
| Rostered | Assigned, not signed on |
| SignedOn | Driver has signed on |
| OnJourney | Working a journey |
| SignedOff | Duty ended normally |
| Orphaned | Duty ended with unsent offline events; requires review (REQ-DRV-008) |

## 7. Vehicle

| State | Meaning |
|---|---|
| Available | Can be assigned |
| Assigned | Assigned to future journeys |
| InService | Working a journey now |
| Failed | Unplanned failure |
| InMaintenance | Planned unavailability window |

## 8. Timetable version

`Draft → InReview → Published → Superseded`. A published version is immutable (REQ-OPS-003). Changes never alter
journeys already generated (REQ-LIVE-017).

## 9. Control message (controller → driver)

`Sent → Delivered → Acknowledged`, with `Expired` if not delivered within its validity. The controller sees the
state; an unacknowledged message is visually distinct (REQ-DRV-007).

## 10. Incident

`Declared → Managed → Resolved → Reviewed`. Severity: Minor, Major, Critical. Only a holder of `incident.declare` (the network controller in the MVP) can declare Critical or move a journey to `TerminatedEmergency`.

## 11. Ticket

```
PendingPayment ──paid──▶ Valid ──board scan──▶ Boarded ──exit scan──▶ Completed
      │                    │                      │
      │                    │                      └──journey ends, no exit scan──▶ Completed (flag)
      │                    ├── journey cancelled / ended before the destination ──▶ Affected ──refund──▶ Refunded
      └──15 min / failed──▶ Expired        └── journey completes, never scanned ──▶ Expired
```

| State | Meaning |
|---|---|
| PendingPayment | Order created, not paid |
| Valid | Paid; not yet scanned in |
| Boarded | Scanned in at the origin gate |
| Completed | Exited, or closed when the journey ended without an exit scan (flagged) |
| Affected | The railway cancelled the journey or ended it before the destination; reachable from Valid or Boarded |
| Refunded | Money returned (a separate refund record has its own states) |
| Expired | Unpaid after 15 min, or the journey ended and the ticket was never used |

## 12. Scan result

Board: `VALID_BOARD`, `ALREADY_BOARDED`, `NOT_VALID_STATUS`, `WRONG_JOURNEY`, `WRONG_DATE`, `WRONG_STATION`, `TAMPERED`.
Exit: `VALID_EXIT`, `NOT_BOARDED`, `OUTSIDE_SEGMENT_PAST_DESTINATION` (with excess fare), `NOT_VALID_STATUS`, `TAMPERED`.
Inspect: `VALID`, `NOT_VALID_STATUS`, `WRONG_JOURNEY`, `WRONG_DATE`, `OUTSIDE_SEGMENT_PAST_DESTINATION`, `OUTSIDE_SEGMENT_BEFORE_ORIGIN`, `TAMPERED`.
Any scanner that cannot reach the server shows `UNABLE_TO_VERIFY` and decides nothing.

## 13. Device (extended)

A **gate scanner** is a device of kind `gate`, scoped to one station, with the same lifecycle as a display (Pending, Online, Stale, Offline,
Revoked). The only action it may take is sending a scan.
