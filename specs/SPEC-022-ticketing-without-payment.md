# SPEC-022 Ticketing core (no payment)

| Field | Value |
|---|---|
| Status | In progress |
| Owner | Malindu |
| Created | 2026-10-19 |
| Approved | 2026-10-19, Malindu ("no payment for the MVP") |
| Branch | `feature/passenger-app` |
| Modules | Ticketing (new), Journeys Contracts (`IJourneySearch`), Identity Contracts, passenger app |
| Changes | New module and schema `ticketing`, API contract, passenger screens |

## 1. Summary

A signed-in passenger chooses a direct train, says who travels, picks seats when the train needs them, confirms, and gets a ticket with a signed QR code that works offline. **No payment is taken** (decision D8); the fare is calculated and shown for information, and a booking is issued as a valid ticket at confirmation.

## 2. Traceability

REQ-TKT-001..005, REQ-TKT-008 (offline ticket), REQ-PAX (tickets). Deferred with payment: REQ-PAY-001..003, REQ-TKT-006/007, REQ-REF-001..005 (see SPEC-023, SPEC-024). Design: 24 to 31, 38, 26 (Add ticket).

## 3. Scope

- `GET /api/ticketing/offers` (public): direct trains with fare per category and `seatRequired`.
- Seats only on routes listed in `Ticketing:SeatReservedRoutes` (default `NORTH`, the long-distance line); coaches A to C, 28 seats. `GET .../journeys/{id}/seats`, `POST /seat-holds` (10 minutes), `POST /bookings` re-checks under a per-train lock (no double booking; overlapping trip parts count).
- Fare: `Base + PerMinute x minutes`, child and senior at half, rounded to 10 (`Ticketing:Fares`); 45 minutes is LKR 320.
- Ticket state is read from the train: **Affected** when the railway cancels or terminates it, **Past** after arrival. No money in the affected sheet: find another train or call the centre.
- QR payload `SR1.{ticketId}.{expires}.{hmac}` signed with `Ticketing:Signing:Key` (a delay never changes it, the ticket is bound to the journey id).
- `POST /tickets/claim`: booking number plus the email used adds a ticket on another phone.
- Ticketing implements `IPersonalDataContributor`: export, and delete keeps the record without the person.

**Out of scope:** payment and refunds, inspector validation (SPEC-025), gates (SPEC-026), multi-leg tickets (a transfer is two bookings), cancellation by the passenger.

## 4. Acceptance criteria

| ID | Criterion |
|---|---|
| AC-1 | Offers show fare per category and whether a seat is needed. |
| AC-2 | A seat-required booking needs a live hold with one seat per traveller. |
| AC-3 | Two people cannot hold or book the same seat on overlapping parts of a trip. |
| AC-4 | A cancelled, finished or departed train cannot be booked. |
| AC-5 | Someone else's ticket looks like no ticket. |
| AC-6 | The ticket shows without a connection with when it was last checked. |
| AC-7 | A cancelled train marks the ticket Affected. |
| AC-8 | Claiming needs both the number and the matching email. |

## 5. Verification

Domain and handler tests, Testcontainers concurrency test for AC-3, widget tests of the flow: run later. Open question: Q1 a booking without payment issues a ticket immediately (draft decision, to be confirmed).

## 6. Change log

- 2026-10-19: created.
