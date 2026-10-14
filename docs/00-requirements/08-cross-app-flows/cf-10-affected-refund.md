# CF-10 — Cancellation reaches tickets, then refund

**Scenario:** J-1042 is cancelled at Mill Lane (terminate short). One passenger holds Riverside → Hill Town
(beyond the termination point), another holds Central → Riverside (before it). This extends
[CF-02](cf-02-cancellation.md): the same controller action now also reaches the ticketing side.

**Requirements:** REQ-TKT-009, REQ-REF-001…004, REQ-PAY-001 · **Test:** E2E-4 (part 2) · **Simulator:** SIM-08

## Timeline

| Time | Actor / component | What happens | Passenger A (to Hill Town) | Passenger B (to Riverside) | Network controller (read-only) |
|---|---|---|---|---|---|
| 08:34:00 | Controller | Terminate J-1042 at Mill Lane, reason "train fault" (CF-02) | — | — | — |
| 08:34:01 | Backend | Journey → `TerminatedShort` at Mill Lane | — | — | — |
| 08:34:02 | Ticketing | Finds Valid tickets on J-1042 with a destination **beyond** Mill Lane → `Affected`, history row written | **Push:** "Your ticket Riverside → Hill Town is affected. Refund available." | Not affected — ticket stays Valid | — |
| 08:35 | Passenger A | Opens the ticket: "Affected — refund available" → **Refund** | Refund requested | — | — |
| 08:35 | Backend | Creates the refund record `REQUESTED`; **auto-approved** (railway-initiated) → `PROCESSING`; calls the provider with an idempotency key | "Refund processing" | — | In list: PROCESSING |
| 08:36 | Provider (fake) | Refund succeeded | — | — | — |
| 08:36 | Backend | Refund → `REFUNDED`; ticket → `Refunded` | "Refunded LKR 120" | — | REFUNDED |

## Why this way

- **Terminate short is not cancel.** Passenger B's ticket does not become invalid just because the train ended later
  than their stop. Only tickets that depend on the lost part of the journey are affected.
- **Refund is a separate record**, so a failed provider call has somewhere to live without corrupting the ticket
  (REQ-REF-002).
- **Auto-approval.** Eligibility after a railway cancellation is a fact, not a judgment; a manual queue adds a screen and
  a delay for no decision.

## Failure branches

| Branch | Behaviour | Edge case |
|---|---|---|
| Passenger taps Refund twice | One refund (unique constraint) | EDG-129 |
| Provider refund call fails | Refund `FAILED`; ticket stays `Affected`; retried automatically (3 attempts, backoff); the passenger can also request again; idempotent | EDG-130, 142 |
| Passenger offline when affected | Push arrives later; the cached ticket shows "last verified" until refreshed; the refund is requested online | EDG-136 |
| Refunded ticket scanned later | Rejected as `NOT_VALID_STATUS` | EDG-131 |
| Rebooking requested | Not available; refund offered | EDG-143 |
| Whole journey cancelled before departure | All Valid tickets on it become Affected | EDG-123 |

**Passengers already Boarded:** if the journey is terminated short before their destination, their ticket also becomes Affected and can be refunded (EDG-158).

## Assertions for E2E-4 (part 2)

1. Only the ticket whose destination is beyond the termination point becomes Affected.
2. One push per affected holder.
3. Exactly one refund per ticket however many times Refund is tapped.
4. With provider failure injected, the refund ends FAILED, is retried automatically, and ends REFUNDED with one provider charge-back.
