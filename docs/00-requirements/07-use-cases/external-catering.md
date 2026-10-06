# External catering — use cases

**Priority decision (ASM-005, user):** catering is low priority. It is built **thin and last**, but kept because
§23 lists "a working integration with a simulated external catering platform" and §26 scores Integration. The
point being demonstrated is the **boundary** — a separate system, a real contract, failure isolation — not food
ordering.

**Ownership:**

| RailBite (external) owns | SmartRail owns |
|---|---|
| Menu, price, stock | Journeys, stations, timetable |
| Order lifecycle and payment (test mode) | Live ETA, cancellations, platform |
| Collection cut-off policy | Notifying the passenger |
| Confirming collection | Nothing about the order beyond a reference |

## Brief §12's eleven questions — answered

| Question (§12) | Answer in the MVP |
|---|---|
| Which stations support catering? | Two seeded stations; eligibility owned by RailBite |
| When can an order be placed? | Up to RailBite's cut-off before scheduled arrival at the collection stop |
| How close to arrival can it be modified? | No modification in MVP — cancel and reorder |
| What if the train is delayed? | SmartRail sends the ETA change; RailBite recalculates its window; passenger notified |
| What if the passenger misses the collection station? | RailBite's policy (hold / forfeit); SmartRail only reports the skip or termination |
| Can an order be cancelled? | Yes, before cut-off, via RailBite |
| Who owns payment? | RailBite, test mode only; SmartRail never sees card data |
| Who owns the menu? | RailBite |
| How are availability changes communicated? | Read at browse time; may be rejected at confirm (handled) |
| How are orders associated with journeys? | Order reference + journey id + collection stop; no passenger identity |
| How is collection confirmed? | RailBite staff mark collected; webhook to SmartRail |

---

### UC-EXT-01 — Provider receives journey and ETA information

| | |
|---|---|
| Actor | RailBite (system) |
| Priority | Could · MVP Thin |

**Main flow**
1. SmartRail sends reference data (stations, journeys with orders) and ETA changes, cancellations, terminations
   and skipped stops for journeys with outstanding orders (REQ-INT-002).
2. Minimal payload: order reference, journey id, collection stop, scheduled and estimated arrival.

**Edge cases:** EDG-113

---

### UC-EXT-02 — Passenger orders food for collection

| | |
|---|---|
| Actor | PER-01 / PER-02 |
| Priority | Could · MVP Thin |

**Main flow**
1. From a journey, "Order food" → eligible collection stops on this journey.
2. Menu from RailBite → choose → hosted test-mode payment page (RailBite's).
3. Order reference stored against the device's follow; passenger automatically follows the journey.

**Failure flows**
- F1 *Stock-out or price change at confirm* — clear message, nothing half-done (REQ-INT-005).
- F2 *RailBite unavailable* — "Food ordering is unavailable right now"; journey features unaffected.

**Requirements:** REQ-INT-005 · **Edge cases:** EDG-115, EDG-110

---

### UC-EXT-03 — Provider reports order status

| | |
|---|---|
| Actor | RailBite |
| Priority | Could · MVP Thin |

**Main flow**
1. RailBite sends `placed`, `confirmed`, `ready`, `collected`, `cancelled`, `refunded` as webhooks.
2. Each is HMAC-signed, timestamped, carries an idempotency key; rejected outside a 5-minute replay window
   (REQ-INT-003).

**Failure flows**
- F1 *Duplicate delivery* — no-op.
- F2 *Bad signature or stale timestamp* — rejected, counted.
- F3 *Out of order (`ready` before `confirmed`)* — state machine tolerates and records.

**Requirements:** REQ-INT-003 · **Edge cases:** EDG-111, EDG-112, EDG-116

---

### UC-EXT-04 — Train delayed or cancelled with orders outstanding

| | |
|---|---|
| Actor | SmartRail, RailBite, passenger |
| Priority | Could · MVP Thin |

**Main flow**
1. Journey delayed → ETA change sent → RailBite adjusts its window → passenger notified (CON-10).
2. Journey cancelled, terminated before the collection stop, or collection stop skipped → event sent →
   RailBite applies its policy → passenger notified.

**Edge cases:** EDG-113, EDG-114 · **Flows:** CF-01, CF-02

---

### UC-EXT-05 — Provider is down

| | |
|---|---|
| Actor | SmartRail |
| Priority | Could · MVP Thin |

**Main flow**
1. Outbound events stay in the outbox; retried with backoff; dead-lettered after the limit (REQ-INT-004).
2. Railway functions — every other use case in this folder set — are unaffected.
3. Reconciliation on recovery (Could).

**Edge cases:** EDG-110
