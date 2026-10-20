# SPEC-016 Journey search A to B with one transfer

| Field | Value |
|---|---|
| Status | In progress |
| Owner | Malindu |
| Created | 2026-10-19 |
| Approved | 2026-10-19, Malindu ("yes implement those") |
| Branch | `feature/passenger-app` (backend in `src/Modules/Journeys`, screens in `apps/mobile/apps/passenger`) |
| Modules | Journeys (owner), Network (names), passenger app |
| Changes | API contract, passenger screens Plan and Results |

## 1. Summary

A passenger chooses From and To and a time, and gets the trains that get them there: **direct** trains and trains with **one transfer**, soonest first. Each option shows departure, arrival, duration, platform, and live state. A transfer shows where to change and says plainly when the connection is **at risk** (REQ-PAX-006).

## 2. Traceability

| Kind | IDs |
|---|---|
| Requirements | REQ-PAX-006 (direct and one-transfer connections, connection at risk), REQ-PAX-005 (state in words), REQ-NFR-005 (network time) |
| Assumptions | ASM-025 (minimum transfer 5 minutes at every station) |
| Design | Passenger app: Plan, Results (Figma "Passenger app"), decision D5 (Results v2 plus v1 extras) |

## 3. Scope

**In scope**
- `GET /api/journeys/search?from=&to=&after=&count=` (public, 5 s cache). Searches today and tomorrow (network days), journeys not yet finished or cancelled.
- Direct options, and one-transfer options where the second train leaves at least 5 minutes after the first arrives.
- Expected times follow the same live rules as the board (estimate only while live data is fresh).
- A connection is **at risk** when the expected arrival plus 5 minutes is later than the expected departure of the second train.
- Plan screen (From, To, swap, depart now or at a time) and Results screen (options, expand a transfer, open a train).

**Out of scope:** more than one transfer, fares, "near me", cheapest or fewest-changes ranking (soonest arrival only), booking from a result (tickets slice).

## 4. Detailed design

- Options are ranked by arrival, then fewer changes. At most `count` (default 8, max 20). A transfer option is dropped when a direct train on the same first leg already arrives earlier.
- Boarding needs a stop that is not Passing or SetDownOnly with a departure; alighting needs a stop that is not Passing or PickUpOnly with an arrival.
- Errors: unknown station -> 404; same From and To -> 400 with a plain message.
- The reader loads journeys visiting From or To; joining is in the application layer (small network).

## 5. Acceptance criteria

| ID | Criterion |
|---|---|
| AC-1 | A direct train from From to To, later than `after`, is returned with scheduled and expected times. |
| AC-2 | A pair of trains with a shared station and at least 5 minutes between arrival and departure is returned as one-transfer. |
| AC-3 | Less than 5 minutes between them is not returned. |
| AC-4 | A connection whose expected times leave less than 5 minutes is returned as at risk. |
| AC-5 | A train that runs the wrong way (To before From) is never returned. |
| AC-6 | Cancelled and completed journeys are never returned. |
| AC-7 | Same From and To, or an unknown station, gives a clear error. |
| AC-8 | The Plan screen needs both stations before it searches; Results shows options, a transfer's changing station and time, and the at-risk warning in words. |
| AC-9 | No result says so, failure can retry, text at 200 percent does not overflow. |

## 6. Verification

Unit tests of the search rules (AC-1..7) in `SmartRail.Modules.Journeys.Tests`; widget tests of Plan and Results (AC-8, AC-9). Run later.

## 7. Change log

- 2026-10-19: created.
