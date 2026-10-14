# CF-11 — Gates and inspection

**Scenario:** A passenger boards J-1042 at Central, rides to Riverside and exits; another travels past their destination; an
inspector spot-checks on board. All scans go through **one validation endpoint**; the server decides, the scanner only shows.

**Why it connects the apps:** the scan is where the **ticket** (what was paid for), the **journey** (the stop order and where the
train is) and the **station** (which gate) meet.

**Requirements:** REQ-GAT-001 … 010, REQ-INS-001 … 006, REQ-TKT-001, REQ-TKT-005 · **Edge cases:** EDG-131 … 135, EDG-141, EDG-149 … 159
· **Simulator:** SIM-21, SIM-22 · **Test:** part of E2E-4

## What every scan carries

| Field | Why |
|---|---|
| QR token (signed) | Proves the ticket is genuine; names the ticket |
| Scan type: `board`, `exit`, `inspect` | Decides which rules apply |
| Device identity (gate) or inspector's current journey and stop | Fixes where the scan happens |
| Client scan id | Makes retries idempotent |
| Time | Logged; **never trusted for decisions** — the server uses its own clock |

The server also knows the journey's ordered stops, so it can say whether one stop is before or after another.

## Happy path — Central to Riverside

| # | Where | Scan | Result | Ticket |
|---|---|---|---|---|
| 1 | Central gate (board) | Passenger A | Origin matches, Valid → **VALID BOARD** | Valid → **Boarded** |
| 2 | On board (inspect) | Inspector, current stop Mill Lane | Segment covers the stop → **VALID** | unchanged |
| 3 | Riverside gate (exit) | Passenger A | Boarded, exit at destination → **VALID EXIT** | Boarded → **Completed** |

## Problems — all recorded, none blocked

| # | Where | Scan | Result shown | Ticket |
|---|---|---|---|---|
| 4 | Central gate | Passenger B scans the same QR again | **ALREADY_BOARDED** | unchanged |
| 5 | Mill Lane gate | Passenger C, ticket to Riverside, exits at Mill Lane (two stops past) | **OUTSIDE YOUR TICKET — excess fare LKR X, including a penalty**; amount logged | Completed, flagged |
| 6 | Hill Town gate | Passenger D scans a Refunded ticket | **NOT VALID — refunded** | unchanged |
| 7 | Central gate | Ticket for another journey | **WRONG_JOURNEY** | unchanged |
| 8 | Any | Altered QR | **TAMPERED** | unchanged |
| 9 | Riverside gate | Exit with no board scan | **NOT_BOARDED** | unchanged |

## After the scan

- **Excess fare:** the Station Master sees it in the excess-fare list, collects it in person, and marks it collected. The system
  takes no payment and blocks nobody (ASM-049, ASM-050).
- **No exit scan:** when the journey completes, a Boarded ticket is closed as Completed, flagged 'no exit scan' (REQ-GAT-006).
- **Cancellation while on board:** a Boarded passenger whose journey is terminated short before their destination has an Affected
  ticket and can be refunded (CF-10, EDG-158).

## Failure branches

| Branch | Behaviour | Edge case |
|---|---|---|
| The gate cannot reach the server | "Can't verify — see staff"; nothing decided or recorded; the Station Master sees it offline within 90 s | EDG-154 |
| The inspector is offline | `UNABLE_TO_VERIFY`; manual procedure; the scan is queued and logged on reconnect | EDG-135 |
| The same scan request is delivered twice | Idempotent; one log entry, same answer | EDG-156 |
| A driver without `ticket.inspect` | 403; the inspector mode is not offered | EDG-141 |
| A Station Master at another station reads the log | 403 | EDG-159 |
| The journey is Unknown (no live position) | The inspector chooses the stop manually; the result notes it | CF-06 |
| The passenger's phone is offline | The cached ticket and QR still display; validation does not need the passenger's connection | EDG-136 |

## What it deliberately does not do

No physical barriers, no fines, no blocking an account, no payment collection, no screenshot-sharing detection beyond the
single-use board scan. Fraud is detected and logged; people handle it.
