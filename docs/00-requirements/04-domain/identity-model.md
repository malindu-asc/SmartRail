# Identity model — what is a "train"?

## Why this needs its own document

The word "train" means at least five different things in conversation, and code that uses one identifier for all
of them breaks in ordinary situations: a locomotive fails and is replaced, the same service runs again tomorrow,
the driver changes halfway, the train arriving as one service departs as another.

## The entities

| Entity | Example | Identity | Lifetime | Owned by | What changes about it |
|---|---|---|---|---|---|
| **Station** | Central Station | station code | Years | Network & Timetable | Facilities, accessibility, closures |
| **Platform** | Central, platform 3 | station + platform code | Years | Network & Timetable | Availability |
| **Route** | Main Line northbound | route id | Years | Network & Timetable | Rarely |
| **Service** | "1042 — 08:00 Central → Hill Town, stops A B C" | service id (within a timetable version) | One timetable version | Network & Timetable | Replaced by a new timetable version, never edited once published |
| **Journey** | 1042 on 2026-10-14 | journey id | One service day | Live Operations | Lifecycle, delay, per-stop times, platforms |
| **Vehicle** (consist) | Trainset S12, or locomotive 841 + 9 coaches | vehicle id | Years | Live Operations (fleet) | Availability, maintenance |
| **Locomotive** | 841 | locomotive id | Years | Live Operations (fleet) | Optional — only for locomotive-hauled trains |
| **Crew duty** | Driver D-07, 06:00–14:00 | duty id | One shift | Live Operations | Signed on / off, handover |
| **Rotation** (diagram) | "S12 works 1042 → 1043 → 1050 today" | rotation id | One service day | Live Operations | Substitutions |
| **Telemetry source** | Feed source "sim-north" | source id | Configured | Live Operations | Key rotation |

## The rule

> **Everything passenger-facing is keyed by Journey.** Vehicles and crews attach to a journey through
> **time-ranged assignments**. Telemetry belongs to a source/vehicle and reaches a journey through the assignment
> in force at the time of the reading.

```
Service ──(instantiated per service date)──▶ Journey ◀──(assignment, from..to)── Vehicle ◀── Telemetry
                                                  ▲
                                                  └──(assignment, from..to)── Crew duty
```

## Why each alternative key fails

| If you key tracking on… | It breaks when… |
|---|---|
| Train number (e.g. "1042") | The same number runs tomorrow; numbers are reused. The physical train arriving as 1042 often departs as 1043. (REQ-LIVE-019) |
| Service id | The same service runs every weekday — today's and tomorrow's positions collide. |
| Vehicle id | A failed locomotive is replaced mid-journey — the journey's history splits across two vehicles. (REQ-LIVE-010) |
| Driver | Crew change at an intermediate station — the journey changes "identity" halfway. (REQ-DRV-009) |
| Device | The driver's phone dies and they continue on a spare. |
| **Journey id** | Never — it is the operational instance, and everything else attaches to it with a time range. |

## Worked examples

### A locomotive fails and is replaced mid-route

- 08:00 journey J-1042-20261014 departs with vehicle **S12** (assignment A1, from 07:55).
- 08:40 S12 fails between stops 3 and 4. Journey → `Interrupted`, ETA → `Unknown` (REQ-LIVE-013).
- 09:25 rescue locomotive **841** attaches. Assignment A1 closes at 09:25; assignment A2 (841) opens at 09:25.
- 09:30 journey → `Running`. Same journey id. Telemetry from 841 now maps to J-1042 via A2.
- Passengers following J-1042 never lose their subscription; the history shows one continuous journey.
- S12's **later** journeys today (1043, 1050) are flagged as having no vehicle (REQ-LIVE-018, REQ-LIVE-011).

### Planned maintenance

- The scheduler marks S12 unavailable 2026-10-20 06:00 to 2026-10-21 18:00 (REQ-OPS-012).
- Every journey assigned to S12 in that window raises "no vehicle" to the scheduler at T-24h and becomes a
  cancellation candidate at T-2h unless reassigned (REQ-LIVE-011).

### Crew handover

- Driver D-07's duty ends at Hill Town; driver D-11 signs on to the same running journey.
- Two duty records attach to one journey. D-07's queued offline events attach to D-07's duty even if they replay
  after D-11 signs on (REQ-DRV-009, REQ-DRV-008).

### The same train, two service numbers

- S12 arrives at the terminus as 1042, turns round, departs as 1043.
- Two journeys, one vehicle, consecutive assignments. A delay to 1042's arrival propagates to 1043's departure
  **through the vehicle**, not the route.

## Working timetable versus public timetable

Railways typically keep a **working timetable** — every movement, including passing times at stations where the
train does not stop and empty-stock movements — and publish a **public timetable** derived from it. SmartRail
stores the working detail and projects the public view:

| Data | Working | Public |
|---|---|---|
| Stops where passengers board / alight | ✓ | ✓ |
| Passing points (no stop) | ✓ | ✗ (hidden from passenger surfaces) |
| Empty-stock and test movements | ✓ | ✗ never shown (REQ-LIVE-015) |
| Pick-up only / set-down only stops | ✓ | ✓ marked |

## Out of scope

- Individual coaches, train formation, splitting and joining (ASM-015). The vehicle is modelled as one consist.
- Track geometry and signalling sections beyond what the simulator needs to place a train between two stops.
