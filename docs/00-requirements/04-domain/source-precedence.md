# Source precedence — when two sources disagree, who wins?

The brief never asks this question, and it is the one that decides whether the system is consistent. As soon as
there is more than one writer, the same fact can arrive twice with different values. Without a rule the system
flip-flops; with an unwritten rule it is unpredictable.

**Principle:** each fact has one authoritative source and ordered fallbacks. The losing value is never discarded —
it is kept in the journey's event history, so the disagreement is visible later (REQ-LIVE-006).

| Fact | 1st (authoritative) | 2nd | 3rd | Conflict rule | Who may override | Audited |
|---|---|---|---|---|---|---|
| **Actual departure / arrival time** | Train detection feed | Driver confirmation | — | Feed time wins; driver time kept with its reason code | Controller, with reason (data correction) | Yes |
| **Position between stops** | Train detection feed | Interpolation from last confirmed event + schedule | — | No position for 300 s → `Unknown`; never extrapolated past the next stop | — | No (high volume) |
| **Stop skipped** | Controller decision | Driver report | Feed (passed without stopping) | Controller decision wins; a driver report of an unplanned skip raises an alert | Controller | Yes |
| **Reason for delay** | Driver report | Controller | — | Driver reason kept; controller may reclassify, original retained | Controller | Yes |
| **Cancellation / termination** | Controller decision | — | — | Only a controller (or incident manager for emergencies) can cancel | Incident manager | Yes |
| **Platform** | Controller / station manager (own station) | Timetable default | — | Latest authorised change wins; history kept | — | Yes |
| **Planned times** | Published timetable version | — | — | Immutable; changes only via a new version | Scheduler + approver | Yes |
| **Vehicle assignment** | Scheduler / fleet controller | — | — | Latest authorised assignment wins; time-ranged | — | Yes |
| **Disruption content** | Author (controller / incident manager) | — | — | Latest published version wins; retracted beats any version | Incident manager | Yes |
| **Device time** | Server time | — | — | Device time recorded but never trusted for ordering | — | No |

## Worked conflict cases

| Situation | Resolution |
|---|---|
| Driver reports "departed 08:07:00" offline; feed recorded 08:07:30 | Feed wins: departure 08:07:30. Driver's 08:07:00 and reason "door fault" stored. |
| Feed silent for 4 min; driver reports arrival at stop 5 | Driver confirmation is accepted (2nd source, feed absent). Confidence `Confirmed`. Feed later reports 30 s different: feed wins, history shows both. |
| Driver reports skipping stop 6; no controller decision exists | Recorded as reported skip, flagged to controller as unplanned. Passengers see "not stopping at Riverside" only after the controller confirms, or after the feed shows the train passed without stopping. |
| Controller changes platform 3→5 while station manager changes 3→4 | Optimistic concurrency: the second save is rejected with the current value; the second editor decides (REQ-OPS-010). |
| Two feed sources report the same train at different places | Plausibility check rejects the impossible one; both recorded with source id (REQ-LIVE-005, REQ-SEC-004). |

## Why the feed beats the driver for timing

A person pressing a button records when they pressed it, which can be a minute after the event in a busy cab, and
an offline queue replays late with a device clock that may be wrong. Train detection records when the train
actually occupied or cleared a location. The driver's value is in **what only a person knows** — the reason, the
skip, the incident — not the timestamp.
