# CF-07 — Breakdown and vehicle substitution

**Scenario:** J-1042 runs with trainset **S12**. At 08:40, S12 fails between Mill Lane and Hill Town and cannot
move. A rescue locomotive **841** is sent, couples at 09:25, and the train continues at 09:30. S12 was also
scheduled to work 1043 and 1050 later today.

This is the case that tests the **identity model**: if anything is keyed on the vehicle, the journey's history
splits in two; if the delay is shown as "+n", passengers are counting down to an invented time.

**Simulator:** SIM-06 (breakdown), SIM-07 (substitution) · **Requirements:** REQ-LIVE-010, REQ-LIVE-013,
REQ-LIVE-018, REQ-LIVE-011, REQ-OPS-012

## Timeline

| Time | Event | Journey state | Passenger app | Displays | Ops |
|---|---|---|---|---|---|
| 08:40 | Feed: no movement; driver: delay reason "train fault" | Running, +4 | "+4 · live" | "Expected 08:44" | +4, fault |
| 08:46 | Controller confirms the train cannot continue → **Interrupted** | `Interrupted`, position frozen | "**Stopped near Mill Lane — no estimate**" | "**Delayed — no estimate**" | Interrupted |
| 08:47 | Scheduler marks S12 `Failed` | — | — | — | S12's later journeys (1043, 1050) flagged **no vehicle** (REQ-LIVE-018) |
| 08:47 | Followers | — | Push: "1042 stopped near Mill Lane — no estimate yet" (once) | — | — |
| 09:25 | Locomotive 841 couples; assignment S12 closes 09:25, assignment 841 opens 09:25 | still `Interrupted` | — | — | Assignment history shows both |
| 09:30 | Controller resumes; feed now reports 841 | `Running`, +50 | "+50 · live" | "Expected 09:30" | Running |
| 09:30 | Followers | — | Push: "1042 moving again, now expected 09:32" (once) | — | — |
| 09:32 | Arrives Hill Town | — | — | — | — |
| 09:40 | Scheduler | Reassigns 1043 to spare trainset S07, cancels 1050 with reason "rolling stock" | — | Updated | Audit entries |

## Why "no estimate" instead of "+n"

During an interruption nobody knows when the train will move. Showing "+6", "+12", "+18" climbing every minute
implies a prediction that does not exist and trains people to distrust every number. "No estimate" is honest, and
the punctuality figure returns only when there is a basis for it (REQ-LIVE-013).

## Identity checks this flow proves

- One journey id from 08:00 to 09:32 despite two vehicles.
- Followers never had to re-subscribe.
- Telemetry from 841 was mapped to J-1042 through the 09:25 assignment, not by vehicle id.
- The rotation cascade reached 1043 and 1050 automatically.

## Out of scope, named

Trains behind J-1042 on the same line are also held. That **knock-on** delay needs a track-capacity model and is
excluded; those journeys simply receive their own feed updates (REQ-LIVE-018).
