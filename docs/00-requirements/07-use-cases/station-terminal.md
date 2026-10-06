# Station terminal — use cases

Same React kiosk codebase in **terminal mode** (ASM-011): interactive, touch, public. Context of use: standing, a
shared screen, people unfamiliar with the network, wheelchair users, the previous person's search possibly still
on screen. No installation, no account, no personal data.

---

### UC-TRM-01 — "How do I get to …?"

| | |
|---|---|
| Actor | PER-02, PER-04 |
| Goal | Find the next way to reach a destination from this station |
| Priority | Must · MVP Yes |

**Main flow**
1. Large on-screen keyboard, destination search with suggestions after two characters.
2. Results: direct trains, then one-change options; live status in the same vocabulary as the app (REQ-TRM-001).
3. Select a result → stops, platform, and a "where to go" hint for the platform.

**Failure flows**
- F1 *Connection at risk* — marked, with the next alternative (REQ-PAX-006).
- F2 *No route within one change* — plain message, and a suggestion to ask staff.

**Requirements:** REQ-TRM-001, REQ-PAX-006 · **Edge cases:** EDG-062

---

### UC-TRM-02 — Station information and facilities

| | |
|---|---|
| Actor | PER-04 |
| Priority | Should · MVP Yes |

**Main flow**
1. Facilities: toilets, ticket office, step-free access, lifts, assistance point, catering collection point.
2. Status of step-free routes where known (REQ-TRM-002).

**Edge cases:** EDG-067

---

### UC-TRM-03 — See disruptions

| | |
|---|---|
| Actor | PER-01, PER-02 |
| Priority | Must · MVP Yes |

**Main flow**
1. Active disruptions affecting this station and its routes, with "planned" works shown separately.

**Requirements:** REQ-PAX-009, REQ-OPS-016 · **Flows:** CF-04

---

### UC-TRM-04 — Leave nothing behind, give nothing away

| | |
|---|---|
| Actor | Device |
| Priority | Must · MVP Yes |

**Main flow**
1. After 60 s without interaction, return to home and clear all input (REQ-TRM-003, ASM-023).
2. No URL bar, external links, file dialogs or browser shortcuts (REQ-TRM-004).
3. No field accepts personal data.

**Failure flows**
- F1 *User walks away mid-search* — cleared at timeout.
- F2 *Someone tries to escape the kiosk* — no reachable route out (manual lockdown checklist).

**Requirements:** REQ-TRM-003, REQ-TRM-004 · **Edge cases:** EDG-085, EDG-086

---

### UC-TRM-05 — Work when the network is down

| | |
|---|---|
| Actor | Device |
| Priority | Should · MVP Yes |

**Main flow**
1. Switches to timetable-only mode with a banner; hides live columns and live-only features (REQ-TRM-005).
2. Station information remains available from cache.

**Requirements:** REQ-TRM-005 · **Edge cases:** EDG-080 · **Flows:** CF-06
