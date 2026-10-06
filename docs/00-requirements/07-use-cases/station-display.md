# Station displays — use cases

React kiosk app in **display mode**: unattended, read-only, no human logs in. Context of use: read from 5–10 m,
glanced at while walking, in a crowd. The industry term for these is a customer information system.

Different locations have different jobs (brief §6):

| Location | Job | Shows |
|---|---|---|
| Platform | "Is the next train on this platform mine?" | Next 3 departures from this platform |
| Concourse / main board | "Which platform, and is it running?" | All departures and arrivals, disruptions |
| Entrance / waiting area | Same as concourse, smaller | Concourse layout, fewer rows |

---

### UC-DSP-01 — Platform display shows what is next on this platform

| | |
|---|---|
| Actor | Device (on behalf of PER-01, PER-02) |
| Priority | Must · MVP Yes |

**Main flow**
1. Shows the next 3 departures from its platform: destination, scheduled time, expected time or `+n`, status
   (REQ-DSP-001).
2. First row emphasised; calling points scroll slowly (within motion limits, REQ-DSP-008).
3. Countdowns use server time, not the device clock (REQ-DSP-006).

**Alternate flows**
- A1 *Platform changed away from here* — row shows "Now departing from platform 5" for the configured time, then
  removed.
- A2 *Platform changed to here* — row appears with "changed from 3".

**Failure flows**
- F1 *Cancelled* — remains as "Cancelled" for 15 min, then removed (REQ-DSP-007).
- F2 *Interrupted* — "Delayed — no estimate", never a made-up time.
- F3 *Device clock wrong* — no effect on displayed minutes.

**Requirements:** REQ-DSP-001, REQ-DSP-006, REQ-DSP-007, REQ-DSP-008 · **Edge cases:** EDG-020, EDG-021, EDG-081, EDG-087
· **Flows:** CF-05

---

### UC-DSP-02 — Concourse board shows the station's departures and disruptions

| | |
|---|---|
| Actor | Device |
| Priority | Must · MVP Yes |

**Main flow**
1. Departures and arrivals, paged when rows exceed the screen (REQ-DSP-002).
2. Active disruptions for the station and its routes in a banner.
3. Status always icon + text + colour (REQ-PAX-004 rule).

**Alternate flows**
- A1 *Station closed* — closure mode: full-screen notice, alternatives (REQ-LIVE-016).

**Requirements:** REQ-DSP-002 · **Edge cases:** EDG-087, EDG-088 · **Flows:** CF-02, CF-04

---

### UC-DSP-03 — Stay honest when the network fails

| | |
|---|---|
| Actor | Device |
| Priority | Must · MVP Yes |
| Flow | **[CF-06](../08-cross-app-flows/cf-06-degraded-data.md)** |

**Main flow**
1. Connection lost → keeps last board; after 60 s shows "Updated hh:mm — live information unavailable"
   (REQ-DSP-004).
2. After 300 s → scheduled times only, "Listen for announcements"; no live markers.
3. Reconnect → jittered backoff, snapshot, then deltas (REQ-DSP-003).

**Failure flows**
- F1 *Hundreds of displays reconnect at once* — jitter spreads the load (EDG-083).

**Requirements:** REQ-DSP-003, REQ-DSP-004, REQ-NFR-007 · **Edge cases:** EDG-080, EDG-083

---

### UC-DSP-04 — Device lifecycle: provision, heartbeat, revoke

| | |
|---|---|
| Actor | Device; PER-12 system administrator |
| Priority | Must · MVP Yes |

**Main flow**
1. Technician enters a one-time pairing code on the device; device receives a read-only token scoped to its
   station and location.
2. Heartbeat every 30 s; offline after 90 s (REQ-DSP-005).
3. Revocation rejects the token on the next request.

**Requirements:** REQ-DSP-003, REQ-DSP-005, REQ-OPS-009 · **Edge cases:** EDG-082, EDG-084
