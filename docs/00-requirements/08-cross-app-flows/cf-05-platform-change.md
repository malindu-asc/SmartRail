# CF-05 — Platform change

**Scenario:** At Hill Town, J-1042 is due on platform 3 at 08:40. At 08:34 platform 3 is blocked by a failed
train, and the controller moves J-1042 to platform 5. About 300 people are on platform 3.

**Why this flow matters in practice:** a platform change is a **crowd movement**, not just a data update. If the
screens flip before staff are in position, hundreds of people move at once down one staircase (INT-03). If the
change reaches people late, they miss the train — and a passenger who needs assistance misses their booking too
(INT-06).

**Simulator:** SIM-14 · **Requirements:** REQ-LIVE-014, REQ-LIVE-020, REQ-PAX-007, REQ-DSP-001

## Timeline

| Time | Actor | What happens | Station manager (console) | Platform 3 display | Platform 5 display | Concourse | Followers |
|---|---|---|---|---|---|---|---|
| 08:34:00 | Controller | Change platform 3 → 5, reason "platform blocked" | — | — | — | — | — |
| 08:34:00 | Console | Warning: platform 5 has J-1050 departing 08:36 (within ±5 min) — warning, not a block (REQ-LIVE-020) | — | — | — | — | — |
| 08:34:10 | Controller | Confirms | **Highlighted:** "1042 now platform 5 (was 3), departs 08:40" | — | — | — | — |
| 08:34:12 | Realtime | Deltas | ✓ | "08:40 Hill Town — **now platform 5**" | "08:40 Hill Town · **Platform changed from 3**" | "Platform 5 (changed from 3)" | **Push immediately:** "1042: now platform 5 (was 3)" |
| 08:36 | — | J-1050 departs platform 5 | — | — | J-1042 moves to top row | — | — |
| 08:40 | — | J-1042 departs | — | row removed | row removed | — | — |

## Design choices and why

| Choice | Reason |
|---|---|
| Publish immediately, station manager sees it at the same moment | A hold-back delay would leave passengers on the wrong platform; the console highlight gives staff the same lead time as passengers, no less (CON-08) |
| "Changed from 3" shown everywhere | Passengers who heard "platform 3" earlier need to see that it *changed*, not just a new number |
| Platform 3 display keeps a "now platform 5" row for a period | People already on platform 3 look at platform 3's screen |
| Push is never throttled for platform changes | Consequential and time-critical (notification matrix) |
| Conflict is a warning | Controllers sometimes do it deliberately; the system informs, the controller decides |

## Failure branches

| Branch | Behaviour |
|---|---|
| Station manager attempts the change at another station | 403 (scoped permission; IDOR negative test) |
| Platform 5 display offline | Shows its last board with "Updated hh:mm"; station manager sees the display offline in device health |
| Change made after boarding started | Allowed; confirmation warns; push marked urgent |
| Changed twice (3 → 5 → 4) | "Changed from 3" reflects the original; history keeps both |
