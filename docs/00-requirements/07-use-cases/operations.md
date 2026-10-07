# Operations console — use cases

React web console used by every staff role. **Station operations is a role-scoped view of this console**, not a
separate application (ASM-010): in the MVP the Network Controller has a station view of the same console. Context of use: desk,
keyboard, long sessions, high information density, decisions under time pressure during disruption.

Authority for every action is in the [permission matrix](../06-requirements/permission-matrix.md). Every
high-consequence action requires a reason code and writes an audit entry (REQ-OPS-004, REQ-OPS-011).

---

### UC-OPS-01 — "What is happening across the network right now?"

| | |
|---|---|
| Actor | PER-06 network controller (also holds the incident, station and reporting capabilities) |
| Goal | A current, honest picture of every journey today |
| Priority | Must · MVP Yes |

**Main flow**
1. Live board lists today's journeys: lifecycle, delay, confidence, vehicle, driver, next stop (REQ-OPS-002).
2. Filters: route, station, state, delay band, confidence.
3. Updates arrive without refresh; each journey shows the age of its data.
4. Drill into a journey for its stop timeline, event history and source of each value.

**Alternate flows**
- A1 *Station view* — the controller filters the board to one station's arrivals and departures.

**Failure flows**
- F1 *Journey goes Unknown* — highlighted; never shown as moving (REQ-LIVE-004).
- F2 *Ghost journey (no feed in active window)* — flagged in a separate list (REQ-LIVE-015).
- F3 *Phantom movement* — shown only to ops, never public.
- F4 *Console loses connection* — banner; actions disabled until reconnected (degraded-mode matrix).

**Requirements:** REQ-OPS-002, REQ-LIVE-004, REQ-LIVE-015 · **Edge cases:** EDG-031, EDG-032, EDG-054, EDG-055, EDG-099
· **Flows:** CF-06

---

### UC-OPS-02 — Cancel, terminate short or reinstate a journey

| | |
|---|---|
| Actor | PER-06 network controller |
| Goal | Change whether a train runs, and have everyone know |
| Authority | journey.cancel / terminate_short / reinstate |
| Priority | Must · MVP Yes |

**Main flow**
1. Controller selects journey → **Cancel** (or **Terminate at …**).
2. Mandatory reason code; optional public message.
3. Confirmation shows who will be affected: followers, stations, displays, catering orders.
4. Confirm → state changes; audit written; propagation per [CF-02](../08-cross-app-flows/cf-02-cancellation.md).

**Alternate flows**
- A1 *Reinstate* — allowed before scheduled departure, with reason; followers notified.
- A2 *Terminate short* — choose the last stop; later stops become Cancelled.

**Failure flows**
- F1 *No reason given* — rejected (REQ-OPS-004).
- F2 *Another controller changed the journey first* — optimistic concurrency conflict shown (REQ-OPS-010).
- F3 *Role lacks permission* (e.g. scheduler, system administrator) — 403, audited attempt.

**Requirements:** REQ-OPS-004, REQ-OPS-010, REQ-OPS-011 · **Edge cases:** EDG-026, EDG-036, EDG-090, EDG-095, EDG-096
· **Flows:** CF-02

---

### UC-OPS-03 — Change a platform

| | |
|---|---|
| Actor | PER-06 network controller |
| Goal | Reallocate a platform and have screens, staff and passengers follow |
| Priority | Must · MVP Yes |

**Main flow**
1. Select journey stop → **Change platform** → new platform → reason.
2. Warning if another journey occupies that platform within ±5 min (warning, not block — REQ-LIVE-020).
3. Publish → platform state `Changed`; the station view highlights it at the same moment; displays show
   "Platform 5 (changed from 3)"; followers pushed immediately ([CF-05](../08-cross-app-flows/cf-05-platform-change.md)).

**Failure flows**
- F1 *A driver or scheduler token tries to change a platform* — 403 (negative test).
- F2 *Change after boarding started* — allowed, but the confirmation states it and the push is marked urgent.

**Requirements:** REQ-LIVE-014, REQ-LIVE-020 · **Edge cases:** EDG-021, EDG-022, EDG-097 · **Flows:** CF-05

---

### UC-OPS-04 — Publish, update and retract a disruption

| | |
|---|---|
| Actor | PER-06 network controller |
| Goal | Tell passengers about a disruption once, consistently, and be able to take it back |
| Priority | Must · MVP Yes |

**Main flow**
1. Create disruption: type, severity, scope (network / route / section / station / journey), message, mandatory
   review-by time.
2. Preview shows exactly how it renders on the passenger app, platform display, concourse display and terminal.
3. Publish → scope resolved to affected journeys and stops (REQ-LIVE-016) → propagation per
   [CF-04](../08-cross-app-flows/cf-04-disruption.md).
4. Update as the situation changes; resolve when over.

**Alternate flows**
- A1 *Network-wide scope* — stays Draft until a **second** authorised person approves (REQ-OPS-005).
- A2 *No second approver available at 03:00* — publish route-scoped disruptions instead (no approval needed);
  practical, auditable, and keeps the second-approver rule meaningful.

**Failure flows**
- F1 *Published in error* — **Retract**: removed everywhere; correction pushed to anyone already notified.
- F2 *Review-by passes with no action* — Expired; owner alerted (REQ-OPS-006).
- F3 *Message contains markup or script* — rendered as plain text (REQ-SEC-005).

**Requirements:** REQ-OPS-005, REQ-OPS-006, REQ-LIVE-016, REQ-SEC-005, REQ-PAX-009 · **Edge cases:** EDG-091, EDG-092, EDG-093, EDG-105
· **Flows:** CF-04

---

### UC-OPS-05 — Draft and publish a timetable version

| | |
|---|---|
| Actor | PER-08 scheduler (author) + a different scheduler (approver) |
| Priority | Must · MVP Yes |

**Main flow**
1. Create draft from the current version; edit services, stops, times, calendars.
2. Submit for review → `InReview`.
3. A different person approves → `Published` with an effective date; previous version `Superseded`.
4. Journeys from the effective date are generated from the new version (REQ-LIVE-008); journeys already generated
   are untouched (REQ-LIVE-017).

**Failure flows**
- F1 *Author approves own version* — rejected.
- F2 *Edit a published version* — rejected; create a new draft.
- F3 *Wrong version published* — publish a corrected version; the bad one is superseded, never edited.

**Requirements:** REQ-OPS-003, REQ-LIVE-008, REQ-LIVE-017 · **Edge cases:** EDG-001, EDG-003, EDG-005

---

### UC-OPS-06 — Make sure every journey has a vehicle and a driver

| | |
|---|---|
| Actor | PER-08 scheduler (holds vehicle availability) |
| Priority | Must · MVP Yes |

**Main flow**
1. Assign vehicles and drivers to journeys (time-ranged assignments).
2. Unassigned journeys appear in a queue at T-24h; at T-2h they are flagged "cancellation candidate" and the
   controller is alerted (REQ-LIVE-011).

**Failure flows**
- F1 *Driver double-booked* — overlapping assignment rejected.
- F2 *Still unassigned at departure* — controller must cancel or reassign; nothing runs "unassigned" silently.

**Requirements:** REQ-LIVE-010, REQ-LIVE-011 · **Edge cases:** EDG-010, EDG-011, EDG-014, EDG-017

---

### UC-OPS-07 — Take a vehicle out of service

| | |
|---|---|
| Actor | PER-08 scheduler |
| Priority | Should · MVP Thin |

**Main flow**
1. Mark vehicle unavailable from / to, with reason (planned maintenance or failure).
2. System lists every journey that loses its vehicle, including later journeys in today's rotation (REQ-LIVE-018).
3. Those journeys enter the unassigned queue (UC-OPS-06).

**Requirements:** REQ-OPS-012, REQ-LIVE-018 · **Edge cases:** EDG-010, EDG-016 · **Flows:** CF-07

---

### UC-OPS-08 — Manage a serious incident

| | |
|---|---|
| Actor | PER-06 network controller (incident capability) |
| Priority | Must · MVP Thin |
| Flow | **[CF-08](../08-cross-app-flows/cf-08-major-incident.md)** |

**Main flow**
1. Declare incident with severity; Critical and emergency termination need a confirmation step, a reason and an audit entry (held by the network controller in the MVP, ASM-047).
2. For a collision, derailment or similar: affected journey → `TerminatedEmergency`; ETA Unknown; public message
   from an **approved template** only.
3. Raw telemetry for the journey placed under legal hold (REQ-OPS-008).
4. Related disruptions (section blocked, station closed) published under the incident.
5. Ownership can be handed over with a note; incident resolved, then reviewed.

**Failure flows**
- F1 *Scheduler, driver or administrator tries to declare Critical* — 403.
- F2 *Shift ends mid-incident* — handover required before the owner can sign off.
- F3 *Sensor silence looks like a crash* — the system never infers an incident; it goes Unknown and alerts.

**Requirements:** REQ-OPS-007, REQ-OPS-008, REQ-DATA-003 · **Edge cases:** EDG-037, EDG-094

---

### UC-OPS-09 — Message a driver and know they got it

| | |
|---|---|
| Actor | PER-06 |
| Priority | Should · MVP Thin |

**Main flow**
1. Compose administrative message to a driver or journey.
2. See `Sent → Delivered → Acknowledged` (REQ-OPS-013).
3. Unacknowledged after validity → alert to use radio.

**Requirements:** REQ-OPS-013, REQ-DRV-007 · **Edge cases:** EDG-049 · **Flows:** CF-03

---

### UC-OPS-10 — Know every screen is working

| | |
|---|---|
| Actor | PER-06 network controller, PER-12 system administrator |
| Priority | Must · MVP Yes |

**Main flow**
1. Device health lists each display and terminal: station, location, state, last heartbeat (REQ-OPS-009).
2. Offline after 90 s is highlighted to the network controller (station view).
3. System administrator provisions a new device with a one-time pairing code, or revokes one.

**Failure flows**
- F1 *Revoked device keeps trying* — rejected on every request; counted.
- F2 *Network controller tries to revoke* — not permitted; requests the administrator.

**Requirements:** REQ-OPS-009, REQ-DSP-003, REQ-DSP-005 · **Edge cases:** EDG-082, EDG-084

---

### UC-OPS-11 — Report punctuality

| | |
|---|---|
| Actor | Network controller |
| Priority | Should · MVP Yes |

**Main flow**
1. Choose period, route, service.
2. On-time percentage using the punctuality bands (ASM-021) and corrected values; CSV export (REQ-OPS-014).

**Requirements:** REQ-OPS-014 · **Edge cases:** EDG-098

---

### UC-OPS-12 — Answer a passenger's question *(deferred 2026-10-06 — role not built; passenger self-service covers the MVP flow)*

| | |
|---|---|
| Actor | PER-10 customer service agent |
| Priority | Could · MVP Thin |

**Main flow**
1. Look up a journey or disruption; see exactly the passenger view plus the reason code (REQ-OPS-015).
2. No write actions.

**Requirements:** REQ-OPS-015 · **Edge cases:** EDG-061

---

### UC-OPS-13 — Manage users and roles

| | |
|---|---|
| Actor | PER-12 system administrator |
| Priority | Must · MVP Yes |

**Main flow**
1. Create staff accounts; assign roles and station scope.
2. Every access change is audited.

**Failure flows**
- F1 *Administrator attempts an operational action* — 403. Separation of duties (REQ-OPS-001).

**Requirements:** REQ-OPS-001, REQ-OPS-011, REQ-SEC-006 · **Edge cases:** EDG-096

---

### UC-OPS-14 — Correct recorded data

| | |
|---|---|
| Actor | PER-06 |
| Priority | Should · MVP Thin |

**Main flow**
1. Select a stop event with a wrong actual time; enter corrected value and reason.
2. Original retained in history; reports use the corrected value.

**Requirements:** REQ-OPS-014, REQ-OPS-011 · **Edge cases:** EDG-098

---

### UC-OPS-15 — Schedule planned engineering works

| | |
|---|---|
| Actor | PER-08 scheduler |
| Priority | Should · MVP Thin |

**Main flow**
1. Create a planned disruption with future start and end and a scope (REQ-OPS-016).
2. Visible on affected journeys and in journey search in advance; activates automatically at start.

**Requirements:** REQ-OPS-016 · **Edge cases:** EDG-074
