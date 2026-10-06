# CF-01 — Delay propagation

**Scenario:** J-1042 is held at a signal for 8 minutes between stop 2 (Riverside) and stop 3 (Mill Lane). A
commuter (PER-01) is following the journey and will board at stop 5 (Hill Town). One catering order is waiting for
collection at stop 4.

**Test:** E2E-1 · **Simulator:** SIM-02 · **Requirements:** REQ-LIVE-005, REQ-LIVE-006, REQ-LIVE-007,
REQ-PAX-002, REQ-PAX-007, REQ-NFR-001, REQ-INT-002

## Timeline

| Time | Actor / component | What happens | Passenger app | Platform display (Hill Town) | Ops console | Catering |
|---|---|---|---|---|---|---|
| 08:12 | Feed | Train passes Riverside on time | "On time · live" | "08:40 Hill Town · On time" | On time | — |
| 08:14 | Feed | Train stopped in section; position updates continue showing no movement | unchanged | unchanged | Speed 0 in section | — |
| 08:17 | Backend | Running delta now +3 (under 5 min change) — ETAs recomputed, no push (ASM-022) | "+3 · live" | "Expected 08:43" | +3 | ETA update sent |
| 08:20 | Driver app | Driver records delay reason **"signal"** (pick-list) | reason shown in detail | — | Reason: signal | — |
| 08:22 | Backend | Delta reaches +8; expected arrival at Hill Town moved ≥ 5 min | **Push:** "1042 now expected at Hill Town 08:48 (+8)" | "Expected 08:48 · Delayed +8" | +8 MinorDelay | ETA update sent; RailBite adjusts its window |
| 08:22 | Notifications | Push also stored in inbox (REQ-PAX-015) | inbox entry | — | — | — |
| 08:24 | Feed | Train moves again | "+8 · live" | unchanged | +8, moving | — |
| 08:26–08:30 | Backend | Further small changes (+9, +7, +8) | values update; **no further push** (throttle) | values update | values update | ETA updates |
| 08:48 | Feed | Arrives Hill Town | "Arrived 08:48" | row clears after departure + 1 min | arrived | — |

## Why it works this way

- **The feed, not the driver, drives timing.** The driver contributes the reason, which only a person knows
  ([source-precedence.md](../04-domain/source-precedence.md)).
- **Delay is a delta on the scheduled time.** Passengers who know "08:40" see that it *was* 08:40 (REQ-PAX-002).
- **One push for one meaningful change.** The 08:26–08:30 jitter would otherwise produce three more pushes
  (CON-05).
- **Catering receives every ETA change** for journeys with outstanding orders, because the provider bears the cost
  of acting late (CON-10).

## Failure branches

| Branch | What changes |
|---|---|
| Feed goes silent at 08:15 | CF-06: stale at 08:16, Unknown at 08:20; app and display revert to scheduled times with a label; no "+n" shown |
| Push provider down at 08:22 | Inbox still updated; retry once; app shows the change on next open |
| Passenger offline at 08:22 | Push arrives when connectivity returns (OS-dependent); app shows cached "+3 · as of 08:17" until refreshed |
| Catering provider down | Outbox retains ETA events; railway surfaces unaffected (REQ-INT-004) |
| Delay grows to +25 | Band changes to MajorDelay; same throttle rules |
| Train turns out to be broken down, not held | CF-07: journey → Interrupted, ETA Unknown, delay figure hidden |

## Assertions for E2E-1

1. With SIM-02, the passenger app shows "+8" within 5 s of the backend accepting the update.
2. Exactly one delay push is received for the 08:17–08:30 sequence.
3. The Hill Town platform display shows "Expected 08:48" with the scheduled 08:40 visible.
4. The ops board shows the driver's reason "signal".
