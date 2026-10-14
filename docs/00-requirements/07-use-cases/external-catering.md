# External catering — use cases

> **Minimal version (ASM-005, 2026-10-07).** A separate service holds a seeded menu and a phone number per catering station. The
> passenger app shows the menu and offers **call to order**. Ordering, payment, collection and any exchange of ETAs or webhooks are
> **not built**: they happen with the provider, by phone. The fuller design is kept below for the record.

**Ownership:**

| RailBite (external) owns | SmartRail owns |
|---|---|
| Menu, categories, prices, station phone numbers | Journeys, stations, timetable, live state |
| The order, the payment, collection | Showing the menu and the number; nothing about orders |

## Brief §12's questions — answered for the minimal version

| Question (§12) | Answer |
|---|---|
| Which stations support catering? | Those in the provider's seeded data |
| When can an order be placed? | By phone with the provider; not through SmartRail |
| Can it be modified or cancelled? | With the provider |
| What if the train is delayed or the passenger misses the stop? | Handled by the provider by phone; SmartRail only shows the live journey |
| Who owns payment? | The provider |
| Who owns the menu? | The provider (RailBite) |
| How are availability changes communicated? | The menu is read from the provider and cached for 5 minutes |
| How are orders associated with journeys and passengers? | They are not; no orders pass through SmartRail |
| How is collection confirmed? | By the provider |

---

### UC-EXT-01 — Provider receives journey and ETA information *(not built)*

Kept for the record. Would send reference data, ETA changes and cancellations for journeys with orders, with no passenger identity.
Rejected for the MVP (REQ-INT-002).

---

### UC-EXT-02 — See the food at a stop and call to order

| | |
|---|---|
| Actor | PER-01 / PER-02 passengers |
| Priority | Could · MVP Thin |

**Main flow**
1. On a journey, a stop with catering shows **Food at Hill Town**.
2. The menu appears grouped by category (meals, snacks, drinks) with prices.
3. A **Call to order** button dials the provider's number for that station.
4. A line states: "Ordering and payment are handled by the catering provider."

**Alternate flows**
- A1 *A stop with no catering* — no food section (EDG-160).

**Failure flows**
- F1 *Menu service down or slow* — "Menu unavailable right now"; the phone number is hidden too (EDG-110, EDG-161).
- F2 *Missing or wrong API key* — 401; nothing shown.

**Requirements:** REQ-INT-001, REQ-INT-004, REQ-INT-005

---

### UC-EXT-03 — Provider reports order status *(not built)*

Signed order webhooks. Rejected for the MVP (REQ-INT-003).

---

### UC-EXT-04 — Train delayed or cancelled with orders outstanding *(not built)*

Would send ETA changes and cancellations to the provider. Rejected for the MVP; delays and cancellations are visible to passengers
in the app as usual.

---

### UC-EXT-05 — The menu service is down

| | |
|---|---|
| Actor | SmartRail; passenger |
| Priority | Could · MVP Thin |

**Main flow**
1. The app asks with a 3-second timeout; the cache covers up to 5 minutes.
2. Past that, the food section shows "Menu unavailable right now".
3. Every other use case in this folder set is unaffected.

**Requirements:** REQ-INT-004 · **Edge cases:** EDG-110, EDG-161
