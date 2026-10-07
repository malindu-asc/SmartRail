# Ticketing, accounts and payments

> **Decision (Malindu, 2026-10-06):** tickets, accounts and payments are **in the MVP**. They are the main flow the
> demo has to show end to end. This was confirmed after the scope risk was raised twice; the build is therefore
> planned as a thin, gated vertical slice (below) rather than debated further.
>
> This reverses earlier decisions: ASM-012 (no passenger accounts), deferred items X-01 and X-02 (ticketing, real
> payments) and OQ-07 (is ticketing in scope). It also **cuts catering completely** (ASM-005) to pay for it.

The brief never mentions tickets, so this section is a **product extension** around a journey, not a brief
requirement. The design principle that keeps it coherent:

> **A ticket is a promise to carry one passenger between two stops of one journey.** It binds to the journey id,
> not to a time. Everything else — delay, platform change, cancellation — is the railway's state changing around a
> promise that stays fixed.

## What is built, and what is documented only

| Built — thin slice (Regional, unreserved) | Documented only (rules kept, nothing built) |
|---|---|
| Passenger accounts: register, sign in; browsing stays anonymous | Intercity / High-speed **reserved** services and seat inventory |
| Buy a single-journey ticket between two stops; Adult / Child / Senior fare | Seat holds, double-booking protection, standing capacity |
| Fake payment provider (default); idempotent, signed confirmation | PayHere sandbox adapter (stretch — needs your sandbox keys) |
| Signed QR ticket in a passenger wallet; available offline | Rebooking onto a replacement service |
| Ticket Inspector mode in the staff Flutter app: scan and validate | Fare adjustment for travelling past the destination |
| Journey cancelled or terminated short → ticket **Affected** + push | Screenshot-sharing / duplicate-use detection |
| Full refund, auto-approved; refund record; Retry for failed refunds | Back-office manual approval queue |
| Ticket history kept; refund is a separate record | Group purchases; season tickets; delay compensation |

## Ticket model

```
Account ──< Ticket >── Journey
              │  origin stop, destination stop, service date, fare category,
              │  price (LKR), needs_assistance flag, signed QR token
              ├──< TicketStatusHistory      append-only, never deleted
              ├──< PaymentAttempt           provider reference, idempotency key
              └──< Refund                   separate record, own states
```

### Ticket states

```
PendingPayment ──payment confirmed──▶ Valid ──journey cancelled / terminated short beyond destination──▶ Affected ──refund completed──▶ Refunded
      │                                  │
      └──15 min / payment failed──▶ Expired        └──journey completed──▶ Expired
```

| State | Meaning | Inspector result |
|---|---|---|
| PendingPayment | Order created, not paid | Not valid |
| Valid | Paid; the journey has not been cancelled | Valid if segment covered |
| Affected | The railway cancelled the journey, or ended it before the destination | Not valid |
| Refunded | Money returned | Not valid |
| Expired | Unpaid after 15 min, or the journey is over | Not valid |

### Refund states (separate record)

`REQUESTED → PROCESSING → REFUNDED`, or `FAILED` (retryable → `PROCESSING`). One active refund per ticket.

### Sales rule

A ticket can be bought while the journey is not Cancelled, and has **not departed the passenger's origin stop**
(actual departure). A delay never creates a new journey and never closes sales early. Availability is checked
when checkout **starts** and again when payment **succeeds**; if the journey became unavailable in between, no
ticket is issued and the payment is refunded automatically.

### Fares

Seeded, stage-based, in LKR: `price = base + rate × stages between origin and destination`, multiplied by the
category: Adult ×1.0, Child ×0.5, Senior ×0.5 (placeholder values — ASM-039). Accessibility is **not** a fare
category: it is a separate `needs_assistance` flag shown to staff, with no assistance workflow in the MVP.

### The QR

The QR carries a signed token: ticket id, journey id, origin, destination, version. **No personal data.** Status is
always looked up server-side. Offline, the signature proves the ticket is genuine but cannot prove it is not
refunded — hence the documented limit that inspector validation needs connectivity.

### Inspection results

`VALID` · `NOT_VALID_STATUS` (Affected / Refunded / Expired / Pending) · `WRONG_JOURNEY` · `WRONG_DATE` ·
`OUTSIDE_SEGMENT_PAST_DESTINATION` · `OUTSIDE_SEGMENT_BEFORE_ORIGIN` · `TAMPERED` · `UNABLE_TO_VERIFY` (inspector
offline). Drivers are **not** inspectors by default; inspection is a separate `ticket.inspect` permission held by a
Ticket Inspector role.

## The 35 decisions

| # | Decision | Build |
|---|---|---|
| 1 | Delay after purchase: scheduled time unchanged, expected time changes; the ticket stays on the same journey id | Build |
| 2 | A delayed train can still be bought until it departs the origin stop; delay never makes a new train | Build |
| 3 | Railway cancellation: `Valid → Affected`; passenger notified; ticket unusable; refund offered | Build |
| 4 | Rebooking is not free reuse: same origin, destination, date, passenger and compatible fare only | Doc |
| 5 | A cancelled ticket cannot become another route | Doc |
| 6 | Reserved-service rebooking issues a new seat and invalidates the old reservation | Doc |
| 7 | Full refund for railway-initiated cancellation; the passenger asks with one tap | Build |
| 8 | **Changed:** auto-approved, because eligibility is a fact. failed refunds **retry automatically** and the passenger can request again; the Network Controller has a read-only view | Build |
| 9 | Refund is a separate record: REQUESTED → PROCESSING → REFUNDED / FAILED | Build |
| 10 | The same ticket cannot be refunded twice (unique constraint plus idempotent provider call) | Build |
| 11 | A ticket covers only its purchased segment | Build |
| 12 | Inspector scan checks segment coverage; fare evasion is detected by inspection, not by magic | Build |
| 13 | Ticket Inspector is a role and permission, as a mode in the staff Flutter app — not a new app | Build |
| 14 | A driver is not automatically an inspector | Build |
| 15 | Validation: signature, status, journey, date, segment (reservation n/a in MVP) | Build |
| 16 | **Reinterpreted:** unreserved tickets have no "consumed" state; reuse is blocked by status | Build |
| 17 | Cancelled / refunded tickets are rejected on scan | Build |
| 18 | Regional = unreserved; Intercity / High-speed = reserved. Only Regional is purchasable | Doc |
| 19 | Reserved services with no reservation are "sold out"; no standing model | Doc |
| 20 | Coach structure ≠ service type; no per-coach capacity system | Doc |
| 21 | Capacity is not consumed before payment — moot for unlimited Regional | Doc |
| 22 | Two passengers cannot hold the same seat | Doc |
| 23 | Payment failure issues no ticket | Build |
| 24 | Duplicate payment callbacks create no duplicate ticket | Build |
| 25 | Unpaid orders expire after 15 min and release nothing (seat holds are documented only) | Build |
| 26 | Adult, Child, Senior are fare categories | Build |
| 27 | Accessibility is a separate flag, not a fare category | Build |
| 28 | **Scoped:** account needed to buy and to see tickets; boards, search and following stay anonymous (brief §14) | Build |
| 29 | A ticket is issued only after confirmed payment | Build |
| 30 | Provider-neutral payment port; fake provider default; PayHere sandbox is a stretch adapter | Build |
| 31 | Ticket and refund history is append-only | Build |
| 32 | Ticket cached on the device with a "last verified" label; **inspector must be online** to check status | Build |
| 33 | Recover tickets by signing in on a new device | Build |
| 34 | Platform change does not affect the ticket; holders auto-follow the journey and get the push | Build |
| 35 | Revalidate availability at checkout start **and** at payment success | Build |

## Gaps found beyond the 35

| Gap | Decision |
|---|---|
| Payment succeeds after the journey was cancelled mid-checkout | No ticket; automatic refund (REQ-TKT-002) |
| Where fares come from | Seeded stage-based table (ASM-039) |
| Group purchases | One ticket per passenger per order |
| When a ticket ends | Expires when its journey completes |
| Personal data | Accounts and payment references are **Personal** data (data-classification.md) |
| Card data | Never touches SmartRail — hosted checkout via the provider |
| Account deletion with tickets and refunds | Personal fields anonymised; financial records retained |

## What this costs, and what pays for it

Estimated **4 to 5 days** including tests: accounts 0.5 · fares and purchase 1 · payment port, fake provider and
idempotent callback 0.75 · QR wallet and offline cache 0.5 · inspector mode 0.75 · affected + auto refund + refund
view 1 · docs and tests 0.5.

| Paid for by | Saves |
|---|---|
| **Catering cut completely** (ASM-005) | ~1–2.5 days |
| Rebooking, reserved seats, manual refund approval not built | ~3 days avoided |
| Journey QR from terminal and display demoted to *Could* | ~0.5 day |

## The gate

Ticketing build starts **after the live-data core works end to end (E2E-1)**. At the D14 feature freeze anything
unfinished is cut to documented-only, with the reason recorded. Order of build if time runs short:

1. accounts + purchase with fake payment + QR ticket (the demo spine)
2. inspector validation
3. cancellation → Affected → refund
4. refund Retry view

## Where each part lives

| Document | Content |
|---|---|
| [06-requirements/requirements.md](../06-requirements/requirements.md) | `REQ-ACC`, `REQ-TKT`, `REQ-PAY`, `REQ-REF`, `REQ-INS` |
| [07-use-cases/ticketing.md](../07-use-cases/ticketing.md) | UC-TKT-01 … 06 |
| [CF-09](../08-cross-app-flows/cf-09-purchase.md) · [CF-10](../08-cross-app-flows/cf-10-affected-refund.md) · [CF-11](../08-cross-app-flows/cf-11-inspection.md) | Purchase, cancellation → refund, inspection |
| [09-edge-cases/catalogue.md](../09-edge-cases/catalogue.md) | EDG-120 … 143 |
| [10-user-stories/stories.md](../10-user-stories/stories.md) | Epic E11, US-100 … 108 |
| [06-requirements/permission-matrix.md](../06-requirements/permission-matrix.md) | Ticket Inspector; refunds recover automatically |
