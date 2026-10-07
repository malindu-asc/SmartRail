# CF-09 — Buying a ticket

**Scenario:** A commuter signs in and buys a Regional ticket Riverside → Hill Town on J-1042 (08:00 Central → Hill
Town). Payment goes through a hosted provider page; the provider confirms by a signed callback.

**This is the main flow of the demo.** It crosses the passenger app, backend, payment provider, notifications and
the live journey state — and it is where an idempotency bug would be visible.

**Requirements:** REQ-ACC-001, REQ-TKT-001…003, REQ-TKT-006…008, REQ-PAY-001…003, REQ-REF-005 · **Test:** E2E-4

## Timeline

| Step | Actor / component | What happens | Passenger app | State |
|---|---|---|---|---|
| 1 | Passenger | Signed in; opens J-1042 and taps **Buy ticket** | Stop pickers, category, price LKR | — |
| 2 | Backend | Checks the journey: not cancelled, **has not departed Riverside** (REQ-TKT-002) | Price confirmed | — |
| 3 | Backend | Creates order `PendingPayment`; payment attempt with an idempotency key; 15 min expiry | "Pay now" | PendingPayment |
| 4 | Payment provider (fake) | Hosted page; success | — | — |
| 5 | Provider → Backend | Signed callback with the provider reference | — | — |
| 6 | Backend | Verifies signature (REQ-PAY-003); **idempotent on provider reference** (REQ-PAY-002) | — | — |
| 7 | Backend | Checks the journey **again** (REQ-TKT-002) — still bookable | — | — |
| 8 | Backend | Ticket → `Valid`; signed QR issued; history row written; journey **auto-followed** | Wallet shows the ticket and QR | **Valid** |
| 9 | Notifications | Confirmation in the inbox | "Ticket ready" | — |

## Failure branches

| Branch | Behaviour | Edge case |
|---|---|---|
| Journey departs Riverside before step 2 | Rejected with reason; nothing charged | EDG-122 |
| **Journey cancelled between steps 3 and 7** | Payment succeeded but **no ticket**; automatic refund; the passenger is told why | EDG-125 |
| Provider calls back twice (steps 5–6) | Second callback is a no-op; one ticket | EDG-127 |
| Callback with a bad signature | Rejected and counted; order unchanged | EDG-128 |
| Payment fails or is abandoned | No ticket; order `Expired` at 15 min | EDG-126 |
| Passenger offline | Cannot start checkout; no partial state | UC-TKT-02 F6 |
| Same seat attempted by two passengers | Not applicable to unreserved Regional; documented for reserved services | EDG-139 |
| Intercity journey selected | Not purchasable message | EDG-139 |

## Assertions for E2E-4 (part 1)

1. After a successful fake payment exactly one ticket exists, `Valid`, with a signed QR.
2. Replaying the same provider callback creates no second ticket and no second charge.
3. The ticket refers to the journey id; a +20 min delay on the journey does not change the ticket.
4. With the journey cancelled between order and confirmation, no ticket is issued and a refund exists.
