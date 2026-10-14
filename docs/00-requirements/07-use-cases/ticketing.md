# Ticketing — use cases

Rules and decisions are in [12-ticketing/README.md](../12-ticketing/README.md). Context of use: the passenger is
buying on a phone, often in a hurry; the inspector is on a moving train with one hand free; refunds retry by themselves, with no one at a desk.

---

### UC-TKT-01 — Create an account and sign in

| | |
|---|---|
| Actor | PER-01, PER-02 |
| Goal | Be able to buy and keep tickets |
| Trigger | First "Buy ticket", or Account |
| Authority | Public → authenticated passenger |
| Priority | Must · MVP Yes |

**Main flow**
1. Email and password; confirmation by code (fake mail provider in the demo).
2. Signed in; tickets screen appears. Browsing, boards and following were never gated.

**Failure flows**
- F1 *Email already used* — neutral message that does not reveal whether the account exists.
- F2 *Weak password / repeated failures* — rejected / temporarily locked.
- F3 *New device* — sign in and all tickets reappear (REQ-ACC-002).

**Requirements:** REQ-ACC-001, REQ-ACC-002 · **Edge cases:** EDG-137

---

### UC-TKT-02 — Buy a ticket for a journey

| | |
|---|---|
| Actor | PER-01, PER-02, PER-04 |
| Goal | Pay for travel between two stops of one journey |
| Trigger | "Buy ticket" on a journey, or from search |
| Preconditions | Signed in; journey is Regional and bookable |
| Priority | Must · MVP Yes |
| Flow | **[CF-09](../08-cross-app-flows/cf-09-purchase.md)** |

**Main flow**
1. Choose origin and destination stops on the journey; passenger category; optional assistance flag.
2. Price shown (LKR). Availability is checked now (REQ-TKT-002).
3. Order created as `PendingPayment`; hosted payment page of the provider opens.
4. Payment confirmed (signed callback) → availability checked **again** → ticket `Valid`, QR issued, journey
   auto-followed.

**Alternate flows**
- A1 *Delayed train* — still purchasable until it departs the origin stop.

**Failure flows**
- F1 *Journey departed origin or cancelled before checkout* — not purchasable, reason shown.
- F2 *Journey cancelled during payment* — no ticket; automatic refund; explained (EDG-125).
- F3 *Payment fails or is abandoned* — no ticket; the order expires after 15 min.
- F4 *Duplicate confirmation* — one ticket (EDG-127).
- F5 *Intercity / High-speed* — "Reservation tickets are not available in this version"; information still shown.
- F6 *Offline at checkout* — purchase requires connectivity; clear message, nothing charged.

**Requirements:** REQ-TKT-001…003, REQ-TKT-006, REQ-TKT-007, REQ-TKT-008, REQ-PAY-001…003, REQ-REF-005 · **Edge cases:** EDG-121, EDG-122, EDG-125, EDG-126, EDG-127, EDG-128, EDG-139

---

### UC-TKT-03 — Show my ticket

| | |
|---|---|
| Actor | PER-01 |
| Goal | Have the ticket ready when asked |
| Priority | Must · MVP Yes |

**Main flow**
1. Wallet lists tickets by journey date; a ticket opens as a large QR with journey, stops and status.
2. The status shown is the ticket's, not the train's: Valid / Affected / Refunded / Expired.

**Alternate flows**
- A1 *Offline* — cached ticket with "last verified hh:mm"; the QR still displays (REQ-TKT-012).
- A2 *Delay or platform change* — journey information updates; the ticket does not (EDG-120, 138).

**Requirements:** REQ-TKT-001, 005, 012, REQ-TKT-010 · **Edge cases:** EDG-120, 136, 138

---

### UC-TKT-04 — My train was cancelled: what happens to my ticket?

| | |
|---|---|
| Actor | PER-01, with PER-06 (controller) |
| Goal | Be told promptly and get my money back |
| Priority | Must · MVP Yes |
| Flow | **[CF-10](../08-cross-app-flows/cf-10-affected-refund.md)** |

**Main flow**
1. Controller cancels or terminates short ([UC-OPS-02](operations.md)).
2. Affected tickets become `Affected` within 5 s; holders are pushed.
3. The ticket shows "Affected — refund available". The passenger taps **Refund**.
4. Refund record created (auto-approved) → PROCESSING → REFUNDED; the ticket becomes `Refunded`.

**Failure flows**
- F1 *Refund tapped twice* — one refund (EDG-129).
- F2 *Provider refund fails* — FAILED; the passenger sees "Refund is being retried"; the system retries automatically (EDG-130).
- F3 *Terminate short* — only tickets whose destination lies beyond the termination point are affected (EDG-124).
- F4 *Rebooking wanted* — "Rebooking is not available in this version"; refund offered.

**Requirements:** REQ-TKT-009, REQ-REF-001…004 · **Edge cases:** EDG-123, 124, 129, 130, 143

---

### UC-TKT-05 — Inspect a ticket on board

| | |
|---|---|
| Actor | Ticket Inspector (permission `ticket.inspect`; **not** the driver by default) |
| Goal | Know in seconds whether a passenger is covered for this part of the journey |
| Priority | Must · MVP Yes |
| Flow | **[CF-11](../08-cross-app-flows/cf-11-inspection.md)** |

**Main flow**
1. Inspector opens the staff app in Inspector mode **on their own phone**; the journey and **current stop** come from the live journey. Inspection is by **spot check**: the gates (UC-GAT-01, 02) cover everyone.
2. Scans the QR; the server validates signature, status, journey, service date and segment.
3. Result in large, simple form: green VALID with name and stops, or red with the reason.

**Failure flows**
- F1 *Affected / Refunded / Expired / Pending* — rejected (EDG-131).
- F2 *Passenger travelled past their destination* — "OUTSIDE SEGMENT — past destination" with the excess fare due; recorded; collected in person (EDG-132).
- F3 *Different journey or date* — rejected (EDG-133).
- F4 *Tampered QR* — rejected (EDG-134).
- F5 *Inspector offline* — "Unable to verify"; scan queued and logged on reconnect (EDG-135).
- F6 *A driver without the permission* — 403 (EDG-141).

**Requirements:** REQ-INS-001…005 · **Edge cases:** EDG-131…135, 141

---

### UC-TKT-06 — A failed refund recovers itself

| | |
|---|---|
| Actor | Backend (automatic); passenger; Network Controller (read-only) |
| Priority | Should · MVP Thin |

**Main flow**
1. The provider refund call fails → the refund is `FAILED`.
2. The backend retries automatically with backoff (3 attempts); the passenger sees "Refund is being retried".
3. A later attempt succeeds → `REFUNDED`; the ticket becomes `Refunded`.

**Alternate flows**
- A1 *Passenger requests again* — idempotent; one provider call.
- A2 *Network Controller* — sees refunds by state, read-only.

**Failure flows**
- F1 *Retry overlaps an in-flight attempt* — idempotent; one provider call (EDG-142).
- F2 *Still failing after 3 attempts* — stays `FAILED`; visible to the Network Controller; the passenger can ask again later.

**Requirements:** REQ-REF-002, REQ-REF-003, REQ-REF-004 · **Edge cases:** EDG-130, EDG-142
