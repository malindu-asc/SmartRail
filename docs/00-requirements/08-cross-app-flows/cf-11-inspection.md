# CF-11 — Ticket inspection

**Scenario:** An inspector on J-1042 at Mill Lane checks four passengers. Each scan shows how the same ticket
data, the live journey and the ticket status combine into one answer.

**Why it connects the apps:** inspection is the only place where the **live journey** (where the train is now) and
the **ticket** (what the passenger paid for) meet. The inspector's current stop comes from the same journey state
every other app shows.

**Requirements:** REQ-INS-001…005, REQ-TKT-001, REQ-TKT-005 · **Edge cases:** EDG-131…135, 141

## Timeline

| # | Passenger holds | Inspector scans at Mill Lane | Result shown |
|---|---|---|---|
| 1 | Central → Hill Town, Valid | Signature OK, status Valid, journey J-1042, date OK, Mill Lane is within the segment | **VALID** — name, stops |
| 2 | Central → Riverside, Valid | Mill Lane is **past** their destination | **OUTSIDE SEGMENT — past destination.** Recorded; fare adjustment outside the system |
| 3 | Central → Hill Town, **Refunded** | Status check | **NOT VALID — refunded** |
| 4 | Ticket for J-1043 (a different journey) | Journey check | **WRONG JOURNEY** |

## Rules

- The inspector's current stop defaults to the journey's live position; it can be corrected by hand.
- The result is decided on the **server** (status can change at any time); the app only shows it.
- Every scan is logged: ticket id, inspector, journey, stop, result, time. Not shown to passengers.
- The QR contains no personal data; the passenger's display name is returned only to an authorised inspector.

## Failure branches

| Branch | Behaviour | Edge case |
|---|---|---|
| Inspector offline | `UNABLE_TO_VERIFY`; the inspector falls back to the manual procedure; scans are queued and logged on reconnect | EDG-135 |
| Tampered or forged QR | `TAMPERED` | EDG-134 |
| A driver without `ticket.inspect` | 403; the Inspector mode is not even offered | EDG-141 |
| Journey is Unknown (no live position) | The inspector selects the stop manually; the result notes it was manual | CF-06 |
| Passenger's phone is offline | Ticket still displays from cache; validation does not need the passenger's connection | EDG-136 |

## What it deliberately does not do

No gates, no boarding or "used" status for unreserved tickets, no detection of a ticket shared by screenshot, no
fare adjustment. A fine or an excess fare is the railway's process, not SmartRail's.
