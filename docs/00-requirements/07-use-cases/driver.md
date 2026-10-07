# Driver app — use cases

Flutter app. Context of use: the cab — gloves, vibration, glare, eyes on the line. **Safety-critical communication
stays on railway radio; this app is never the safety channel (ASM-009).** The app is supplementary to the train
detection feed (ASM-008): it adds what only a person knows, and is a fallback when the feed is missing.

---

### UC-DRV-01 — Sign on and see my duty

| | |
|---|---|
| Actor | PER-05 driver |
| Goal | Know today's journeys, stops and notices |
| Trigger | Start of shift |
| Preconditions | Driver has an organisation account; a duty is rostered |
| Authority | Own duty |
| Priority | Must · MVP Yes |

**Main flow**
1. Driver signs in; PIN or biometric thereafter (REQ-DRV-001).
2. Duty shows assigned journeys with routes, stops, scheduled times and notices (REQ-DRV-002).
3. Driver signs on; duty → `SignedOn`. Data cached for offline use.

**Alternate flows**
- A1 *Shared device* — previous driver's cache is cleared on sign-in.

**Failure flows**
- F1 *No network at sign-on, cache from yesterday* — shows "duty not current" — not yesterday's duty as today's.
- F2 *No network, no cache* — app says so; driver uses the paper/radio fallback the organisation already has.
- F3 *Unsent events from a previous duty* — surfaced for review, not applied to this duty (REQ-DRV-008).
- F4 *No duty rostered* — "No duty assigned — contact control"; nothing invented.

- F5 *Another driver's duty or journey requested* — 403; only the own duty is ever returned.

**Requirements:** REQ-DRV-001, REQ-DRV-002, REQ-DRV-008 · **Edge cases:** EDG-012, EDG-013, EDG-144, EDG-145, EDG-147

---

### UC-DRV-02 — Work a journey and record stop events

| | |
|---|---|
| Actor | PER-05 |
| Goal | Record arrival and departure at each stop with minimal effort |
| Priority | Must · MVP Yes |

**Main flow**
1. Active journey screen: next stop, scheduled time, running delay, large layout.
2. At each stop: one-tap **Arrived**, one-tap **Departed** (REQ-DRV-003).
3. Event sent with device time, server-receive time, sequence, idempotency key, duty id.
4. Feed timing and driver timing reconciled by precedence (feed wins for time) (REQ-LIVE-006).

**Alternate flows**
- A1 *Driver forgets to tap* — feed still records the movement; no passenger impact.
- A2 *Taps Departed twice* — idempotent; one event.

**Failure flows**
- F1 *Between stops* — free-text disabled; only one-tap and pick-list actions (REQ-DRV-004).
- F2 *Device clock wrong* — event accepted with downgraded confidence; server time used for ordering.
- F3 *Wrong stop or out-of-order event* — validated against the stop sequence; rejected or held for correction, never applied silently.

**Requirements:** REQ-DRV-003, REQ-DRV-004, REQ-DRV-011, REQ-LIVE-006 · **Edge cases:** EDG-041, EDG-042, EDG-146

---

### UC-DRV-03 — Say why we're late, or that we're not stopping

| | |
|---|---|
| Actor | PER-05 |
| Goal | Record cause of delay or a skipped stop, so delay isn't misattributed |
| Priority | Must · MVP Yes |

**Main flow**
1. Driver taps **Delay reason**; picks from a short reason-code list (signal, door fault, crew, passenger incident,
   track, other).
2. Reason attached to the journey; controller may reclassify, original kept (source precedence).

**Alternate flows**
- A1 *Skip stop* — pick-list reason; recorded as reported skip; controller alerted; passengers informed only after
  controller confirmation or feed evidence ([source-precedence.md](../04-domain/source-precedence.md)).

**Requirements:** REQ-DRV-003 · **Edge cases:** EDG-043

---

### UC-DRV-04 — Keep working when the phone loses signal

| | |
|---|---|
| Actor | PER-05 |
| Goal | Record events without coverage and have them land correctly later |
| Priority | Must · MVP Yes |
| Flow | **[CF-03](../08-cross-app-flows/cf-03-driver-offline.md)** |

**Main flow**
1. Connectivity lost → explicit offline banner, last-sync time, pending count (REQ-DRV-005).
2. Actions queue locally (persisted; survives app kill).
3. Banner warns: "Messages from control may be missing since 08:04" (REQ-DRV-007).
4. On reconnect, queue replays in order; server de-duplicates; feed timing wins; reasons kept (REQ-DRV-006).

**Failure flows**
- F1 *App killed while offline* — queue survives; replays on next launch.
- F2 *Battery dies; duty ends* — events become orphaned; reviewed on next sign-on (REQ-DRV-008).
- F3 *Replay after another driver signed on* — events attach to the original duty (REQ-DRV-009).
- F4 *Events outside the duty window* — flagged, not applied silently.

**Requirements:** REQ-DRV-005…009 · **Edge cases:** EDG-044…048

---

### UC-DRV-05 — Receive and acknowledge a message from control

| | |
|---|---|
| Actor | PER-05, with PER-06 controller |
| Goal | Get administrative instructions and confirm receipt |
| Priority | Must · MVP Yes |

**Main flow**
1. Controller sends a message; state `Sent`.
2. App receives it; state `Delivered`; shown prominently at the next stop (not mid-section if possible).
3. Driver taps **Acknowledge**; state `Acknowledged`; controller sees it (REQ-DRV-007, REQ-OPS-013).

**Failure flows**
- F1 *Driver offline* — stays `Sent`; controller sees it unacknowledged; driver app warns of possible missed
  messages.
- F2 *Message expires undelivered* — controller alerted to use radio.

**Requirements:** REQ-DRV-007, REQ-OPS-013 · **Edge cases:** EDG-049

---

### UC-DRV-06 — Request assistance

| | |
|---|---|
| Actor | PER-05 |
| Goal | Ask control for help (medical, overcrowding, unsafe passenger situation) |
| Priority | Should · MVP Yes |

**Main flow**
1. **Request assistance** → pick type → **confirm** (prevents accidental trigger).
2. High-priority alert to the controller with journey and last known position.
3. Does **not** change journey state by itself; the controller decides.

**Failure flows**
- F1 *Offline* — queued and marked urgent; driver told to use radio. This app is never the only route for an
  emergency.

**Requirements:** REQ-DRV-003 · **Edge cases:** EDG-050

---

### UC-DRV-07 — Hand over to another driver mid-journey

| | |
|---|---|
| Actor | PER-05 (outgoing and incoming) |
| Priority | Should · MVP Thin |

**Main flow**
1. Outgoing driver signs off the journey at a stop.
2. Incoming driver signs on to the running journey.
3. Two duty records attach to one journey; handover logged (REQ-DRV-009).

**Failure flows**
- F1 *Both drivers appear signed on* — the outgoing duty must be closed first; events stay with their own duty.

**Requirements:** REQ-DRV-009 · **Edge cases:** EDG-047, EDG-148
