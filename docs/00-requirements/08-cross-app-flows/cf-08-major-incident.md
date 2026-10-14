# CF-08 — Major incident (collision or derailment)

> **SmartRail is an information system, not a safety system (ASM-009).** Emergency response, protection of the
> line and communication with the driver happen through the railway's safety systems and radio. SmartRail's job
> in a serious incident is to **stay accurate, stay quiet where it doesn't know, and preserve evidence.**

**Scenario:** J-1042 is involved in a collision at a level crossing between Mill Lane and Hill Town.

**Simulator:** SIM-17 · **Requirements:** REQ-OPS-007, REQ-OPS-008, REQ-DATA-003, REQ-LIVE-016

> **MVP note:** the incident capability is held by the Network Controller (ASM-047); a production system would split it into a dedicated Incident Manager role.

## What the system must never do

| Never | Why |
|---|---|
| Infer an accident from sensor data | A silent feed is usually a feed fault. Automated "crash" messages would be wrong far more often than right, and catastrophic when wrong. Silence → `Unknown`, nothing more. |
| Publish anything speculative | Public messaging in a serious incident carries legal and human weight (INT-08) |
| Continue showing an ETA | There is none |
| Delete the raw data on the normal 7-day schedule | An investigation will need it |

## Timeline

| Time | Actor | What happens | Public surfaces | Ops |
|---|---|---|---|---|
| 10:12 | Feed | Train stops in section | "+0 · live" | — |
| 10:13 | Driver | (Radio to signaller — outside SmartRail.) Driver app: **Request assistance** → confirm | — | High-priority alert to controller |
| 10:15 | Controller | Declares incident (Major) and marks J-1042 `Interrupted` | "Stopped — no estimate" | Incident created |
| 10:18 | Network controller | Escalates to **Critical**; moves J-1042 to `TerminatedEmergency` | **Approved template:** "This service has been stopped due to an incident. Please follow staff instructions." | Needs a confirmation step, a reason and an audit entry; scheduler and driver tokens get 403 |
| 10:18 | Backend | Raw telemetry for J-1042 placed under **legal hold** | — | Hold visible on the incident |
| 10:20 | Network controller | Publishes section-blocked disruption under the incident; affected journeys resolved automatically | Disruption banners; affected trains cancelled or terminated short | — |
| 10:30 | Station Masters | See their affected stations; may set station-specific notices | Station notices | — |
| 14:00 | Network controller | Shift handover: ownership reassigned with a note | — | Handover logged |
| 18:00 | Network controller | Line reopens; disruption resolved; incident → `Resolved` | Banners removed | Review pending |

## Failure branches

| Branch | Behaviour |
|---|---|
| Scheduler, driver or administrator attempts `TerminatedEmergency` | 403 |
| Driver mis-taps "request assistance" | Confirm step prevents it; if confirmed in error, the controller cancels the request — no public effect |
| Feed silence mistaken for an incident | Impossible by design; the journey goes `Unknown` and operations is alerted |
| Retention purge runs that night | Held telemetry is excluded (REQ-DATA-003) |
| Incident owner's shift ends | Incident cannot be left without an owner |

## Out of scope

Emergency-services integration (manual flag only), passenger manifests (no ticketing), audio announcements.
