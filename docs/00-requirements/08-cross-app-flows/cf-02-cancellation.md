# CF-02 — Cancellation of a running journey

**Scenario:** J-1042 has a vehicle fault at Mill Lane (stop 3). The network controller (PER-06) decides to
terminate it there and cancel the remainder. Passengers are waiting at stops 4–6; a commuter follows the journey; a
catering order is due for collection at stop 4.

**Test:** E2E-3 · **Simulator:** SIM-08 · **Requirements:** REQ-OPS-004, REQ-OPS-010, REQ-OPS-011,
REQ-LIVE-001, REQ-PAX-007, REQ-DSP-007, REQ-INT-002

## Timeline

| Time | Actor / component | What happens | Passenger app | Displays (stops 4–6) | Driver app | Catering |
|---|---|---|---|---|---|---|
| 08:31 | Driver app | Driver reports delay reason "train fault" at Mill Lane | "+3" | "+3" | reason recorded | — |
| 08:33 | Controller | Opens journey → **Terminate at Mill Lane** | — | — | — | — |
| 08:33 | Ops console | Requires reason code ("train fault"); shows impact: 1 follower, 3 stations, 7 displays, 1 catering order | — | — | — | — |
| 08:34 | Controller | Confirms | — | — | — | — |
| 08:34 | Backend | Journey → `TerminatedShort` at Mill Lane; stops 4–6 → `Cancelled`; **audit entry** with actor, reason, before/after (REQ-OPS-011) | — | — | — | — |
| 08:34 (≤ 5 s) | Realtime | Deltas to all connected clients | **Push:** "1042 terminates at Mill Lane — train fault. Not calling at Hill Town." | "Cancelled" (stops 4–6) | "Journey terminated at Mill Lane" message | Termination event; RailBite applies its policy |
| 08:34 | Ops console | Station managers at stops 4–6 see the change highlighted | — | — | — | — |
| 08:35 | Passenger | Opens app → alternatives via journey search | next train listed | next train shown | — | — |
| 08:49 | Displays | Cancelled rows retained 15 min after scheduled departure, then removed (ASM-024) | — | removed | — | — |

## Why it works this way

- **One action, every surface.** The controller says it once (INT-02). There is no second system to update.
- **Impact preview before confirming.** The controller sees who will be affected — the most expensive mistakes are
  the ones made without seeing their reach.
- **Cancelled trains stay visible.** A train that silently disappears from a board makes passengers think they
  misread it. "Cancelled" for 15 minutes is unambiguous.
- **Reason code is mandatory.** It feeds the passenger message, customer service and punctuality reporting.

## Failure branches

| Branch | What happens |
|---|---|
| Another controller edited J-1042 at 08:33 | Second save rejected with the current version (REQ-OPS-010) |
| Controller lacks permission (e.g. a scheduler) | 403, attempt audited |
| Driver app offline at 08:34 | Message stays `Sent`; controller sees it unacknowledged; driver app shows "messages may be missing" on reconnect (CF-03) |
| A display at stop 5 is offline | It shows "Updated 08:30 — live information unavailable" and never claims the train is on time (CF-06); station manager sees the display offline |
| Wrong journey cancelled | Reinstate (before scheduled departure) or re-plan; reinstatement is pushed to followers too |
| Catering provider down | Termination event retained in outbox; railway unaffected |

## Assertions for E2E-3

1. Controller action produces exactly one audit entry with reason and before/after.
2. Passenger app, platform display and terminal all show the cancellation within 5 s.
3. The follower receives one push naming the termination point.
4. A scheduler's token on the same endpoint receives 403.
