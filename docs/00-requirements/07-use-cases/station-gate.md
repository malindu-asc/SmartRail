# Station gates — use cases

The gate scanner is the kiosk app in **gate mode** (ASM-011, ASM-048). In the MVP a gate is a screen, not a barrier: it scans a
QR, asks the server, and shows the answer. Context of use: a crowd at rush hour, a few seconds per person, staff watching.

---

### UC-GAT-01 — Board with a scan

| | |
|---|---|
| Actor | PER-01, PER-02 passengers; the gate device |
| Goal | Board without a queue at a person |
| Priority | Must · MVP Yes |
| Flow | **[CF-11](../08-cross-app-flows/cf-11-inspection.md)** |

**Main flow**
1. The passenger shows the QR. The gate scans it and sends the token, scan type `board`, its own identity (which fixes the station) and a scan id.
2. The server checks signature, status, journey, date and that this is the ticket's origin.
3. The ticket becomes **Boarded**; the screen shows a large green result with the stops.

**Failure flows**
- F1 *Same QR scanned again* — `ALREADY_BOARDED` (EDG-149).
- F2 *Wrong station* — `WRONG_STATION` (EDG-150).
- F3 *Affected, refunded or expired ticket* — rejected with the reason and, if affected, a pointer to the refund (EDG-155).
- F4 *Scan retried by a flaky connection* — idempotent; one log entry (EDG-156).

**Requirements:** REQ-GAT-001, REQ-GAT-002, REQ-GAT-004, REQ-GAT-007

---

### UC-GAT-02 — Exit with a scan

| | |
|---|---|
| Actor | Passenger; the gate device |
| Priority | Must · MVP Yes |

**Main flow**
1. The passenger scans at the exit gate (`exit`).
2. The server checks the ticket is Boarded and the exit station is at or before the destination in the journey's stop order.
3. The ticket becomes **Completed**.

**Alternate flows**
- A1 *Early exit* — allowed; Completed and flagged (EDG-157).

**Failure flows**
- F1 *Past the destination* — the screen shows "Outside your ticket. Excess fare LKR X, including a penalty." The ticket is Completed with a flag; the amount is logged; nothing is charged or blocked. Staff collect it in person (EDG-151).
- F2 *No board scan* — `NOT_BOARDED` (EDG-152).

**Requirements:** REQ-GAT-003, REQ-GAT-005, REQ-GAT-007

---

### UC-GAT-03 — The gate cannot reach the server

| | |
|---|---|
| Actor | Gate device; Station Master |
| Priority | Must · MVP Yes |

**Main flow**
1. The gate shows "Can't verify — see staff". It decides nothing and records no scan (EDG-154).
2. After 90 s the Station Master sees the gate offline.

**Requirements:** REQ-GAT-008, REQ-DSP-005 · **Flow:** CF-06

---

### UC-GAT-04 — Handle a problem at the gate in person

| | |
|---|---|
| Actor | PER-07 Station Master; station staff |
| Priority | Must · MVP Yes |

**Main flow**
1. A failed scan or an excess fare appears on the gate screen and in the scan log.
2. Staff deal with the passenger in person, using the railway's own procedure.
3. The Station Master marks an excess fare as collected, or leaves it open.

**Not in the system:** fines, blocking an account, taking payment, appeals (ASM-050).

**Requirements:** REQ-GAT-009, REQ-GAT-010, REQ-OPS-018
