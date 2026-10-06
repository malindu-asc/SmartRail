# Passenger app — use cases

Flutter app, anonymous by default (ASM-012). Context of use: walking, one hand, sunlight, seconds of attention.

---

### UC-PAX-01 — "When is my next train from here?"

| | |
|---|---|
| Actor | PER-01 commuter, PER-02 occasional traveller |
| Goal | See upcoming departures from a station and decide which to take |
| Trigger | Opens the app, or taps a saved station |
| Preconditions | None — no sign-in |
| Authority | Public |
| Priority | Must · MVP Yes |

**Main flow**
1. App opens on the last-used or saved station's board.
2. Board lists departures sorted by expected time: destination, scheduled time, expected time or `+n`, platform,
   status, confidence marker (REQ-PAX-002, REQ-PAX-004).
3. Board updates in place as changes arrive; rows do not jump under the user's finger (accessibility).
4. Passenger taps a departure to open its journey (UC-PAX-02).

**Alternate flows**
- A1 *Choose another station* — search by name; recent and saved stations first.
- A2 *Platform not yet assigned* — shows "Platform —" with "to be announced" accessible label.
- A3 *Train running early* — arrival may show early; departure never shown earlier than scheduled (REQ-LIVE-012).

**Failure flows**
- F1 *Data stale (60 s+)* — rows show "as of hh:mm" (REQ-PAX-005).
- F2 *Journey Unknown* — row shows "No live data — scheduled 14:25", no delay figure.
- F3 *Phone offline* — last board with age; banner "Offline"; refresh retries (REQ-PAX-010).
- F4 *Cancelled train* — row stays, marked "Cancelled" with reason, for 15 min (REQ-DSP-007 rule applied in-app).
- F5 *Interrupted train* — "Stopped — no estimate", not "+n" (REQ-LIVE-013).

**Requirements:** REQ-PAX-001, REQ-PAX-002, REQ-PAX-004, REQ-PAX-005, REQ-PAX-010, REQ-LIVE-012, REQ-LIVE-013
**Edge cases:** EDG-020, EDG-024, EDG-031, EDG-060, EDG-061 · **Flows:** CF-01, CF-06

---

### UC-PAX-02 — "Where is my train and when does it reach my stop?"

| | |
|---|---|
| Actor | PER-01, PER-02, PER-03 |
| Goal | Follow a running journey's progress and see the ETA at a chosen stop |
| Trigger | Opens a journey from a board, search or a notification |
| Preconditions | Journey exists for the service date |
| Authority | Public |
| Priority | Must · MVP Yes |

**Main flow**
1. Journey detail shows a schematic stop timeline: each stop with scheduled, estimated and actual times, per-stop
   status, platform (REQ-PAX-003).
2. Current position shown as "at Riverside" or "between Riverside and Hill Town".
3. Header composes lifecycle, punctuality and confidence ("Running · +8 · live").
4. Disclaimer that live information is indicative (REQ-PAX-011).

**Alternate flows**
- A1 *Skipped stop* — shown struck through, "not stopping"; next stop advances past it.
- A2 *Vehicle substituted* — no visible break; journey continues (REQ-LIVE-010).
- A3 *Service crosses midnight* — remains listed under its service date (REQ-LIVE-009).
- A4 *Same train number tomorrow* — separate journey; following today's does not follow tomorrow's (REQ-LIVE-019).

**Failure flows**
- F1 *Interrupted* — position frozen with "last known"; ETA "Unknown".
- F2 *Terminated short* — stops after the termination point marked cancelled; message names the last stop.
- F3 *Emergency termination* — approved template text only; no speculation (REQ-OPS-008).
- F4 *Unknown confidence* — timeline shows scheduled times, clearly labelled.

**Requirements:** REQ-PAX-003, REQ-PAX-011, REQ-LIVE-001…004, REQ-LIVE-009, REQ-LIVE-010, REQ-LIVE-013, REQ-LIVE-019
**Edge cases:** EDG-030, EDG-033, EDG-034, EDG-036, EDG-002 · **Flows:** CF-01, CF-07, CF-08

---

### UC-PAX-03 — "How do I get from A to B?"

| | |
|---|---|
| Actor | PER-02, PER-04 |
| Goal | Find a direct train or a one-change connection |
| Trigger | Enters origin and destination |
| Authority | Public |
| Priority | Should · MVP Yes |

**Main flow**
1. Results list direct trains, then one-transfer itineraries respecting 5-minute minimum transfer (ASM-025).
2. Each leg shows live status.
3. Passenger opens an itinerary or follows its legs.

**Alternate flows**
- A1 *Planned works on a leg* — itinerary marked "planned disruption" (REQ-OPS-016).
- A2 *Accessibility filter* — step-free stations only (Could).

**Failure flows**
- F1 *First leg delay erodes the transfer* — connection marked "at risk", with the next alternative (REQ-PAX-006).
- F2 *Second leg cancelled* — itinerary marked broken; alternatives offered.
- F3 *No route within one change* — honest "No direct or one-change journey found", not an empty screen.

**Requirements:** REQ-PAX-006, REQ-OPS-016 · **Edge cases:** EDG-062, EDG-063

---

### UC-PAX-04 — "Tell me if anything changes with my train"

| | |
|---|---|
| Actor | PER-01, PER-03 |
| Goal | Receive timely, non-spammy notifications about one journey |
| Trigger | Taps "Follow" on a journey |
| Preconditions | Push permission granted, or inbox-only |
| Authority | Own device subscription |
| Priority | Must · MVP Yes |

**Main flow**
1. App registers an anonymous device subscription for that journey (REQ-PAX-007).
2. Changes are notified per the [notification matrix](../06-requirements/notification-matrix.md).
3. Every notification is also in the in-app inbox (REQ-PAX-015).
4. Follow expires automatically after the journey completes.

**Alternate flows**
- A1 *Push permission denied* — inbox only, explained once.
- A2 *Follow several journeys* — each throttled independently.

**Failure flows**
- F1 *Six delay updates in five minutes* — one push (ASM-022).
- F2 *Push provider down* — inbox still updated; retry once.
- F3 *Disruption retracted after a push* — correction push (REQ-PAX-009).
- F4 *Token expired* — subscription cleaned up; app re-registers on next open.

**Requirements:** REQ-PAX-007, REQ-PAX-008, REQ-PAX-009, REQ-PAX-015 · **Edge cases:** EDG-070, EDG-071, EDG-072
· **Flows:** CF-01, CF-02, CF-04

---

### UC-PAX-05 — "My train is disrupted — what now?"

| | |
|---|---|
| Actor | PER-01, PER-02 |
| Goal | Understand a disruption and decide what to do |
| Trigger | Notification or banner |
| Authority | Public |
| Priority | Must · MVP Yes |

**Main flow**
1. Banner shows scope, severity, message, last updated (REQ-PAX-009).
2. Affected journeys show their new state (cancelled, delayed, terminated short).
3. Passenger opens journey search for alternatives (UC-PAX-03).

**Failure flows**
- F1 *Disruption retracted* — banner disappears; "Correction" shown if previously notified.
- F2 *Disruption past review-by* — still shown, ops warned; passengers see the published version only.
- F3 *Conflicting information between app and board* — impossible by design (one state); if observed, it is a defect
  with high severity.

**Requirements:** REQ-PAX-009, REQ-OPS-005, REQ-OPS-006 · **Flows:** CF-02, CF-04

---

### UC-PAX-06 — "I'm in a tunnel / the signal is bad"

| | |
|---|---|
| Actor | PER-01 |
| Goal | Still see useful, honestly-labelled information |
| Priority | Should · MVP Yes |

**Main flow**
1. App detects no connectivity; shows cached data with its age (REQ-PAX-010).
2. Live markers disappear; nothing claims to be live.
3. On reconnect, snapshot refresh, then live updates resume.

**Failure flows**
- F1 *Cache older than 24 h* — not shown; "No saved information".
- F2 *Follow tapped while offline* — queued and confirmed when back online.

**Requirements:** REQ-PAX-010, REQ-PAX-005 · **Flows:** CF-06

---

### UC-PAX-07 — "Delete everything about me"

| | |
|---|---|
| Actor | Any passenger |
| Goal | Remove follows and device data |
| Priority | Must · MVP Yes |

**Main flow**
1. Settings → Privacy → Delete my data.
2. Confirmation; server deletes subscriptions and token; app clears local cache.

**Failure flows**
- F1 *Offline* — deletion queued, clearly stated as pending.

**Requirements:** REQ-PAX-012 · **Data:** [data-classification.md](../06-requirements/data-classification.md)
