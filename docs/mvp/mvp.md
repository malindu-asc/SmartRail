# SmartRail MVP

Brief §25: choose an achievable scope, prioritise it, document what is deferred and why. About 12 days remain.

## Definition

The minimum viable product is the smallest version that works end to end for real: a train's state flows from the simulator to the people who use it, every screen shows it truthfully, and a controller can change the service.

## Roles in the MVP

| Role | Primary need in the system |
|---|---|
| **Passenger** | Reliable live train information is the reason the system exists |
| **Driver** | The only human source of what a train is doing |
| **Network Controller** | The only role that can change a running service |
| **System Administrator** | Someone must control who may change a running service, apart from those who do it |

## Roles deferred

| Role | Why not in the MVP |
|---|---|
| Scheduler | Timetables and assignments are seeded; editing is documented only |
| Ticket Inspector | Needs tickets first; gates can validate the same endpoint |
| Station Master | A scoped view of the console; adds no new mechanism |
| Incident Manager, Reporting Analyst | Capabilities of the Network Controller |
| Fleet Controller | Covered by scheduling |
| Customer Service | The call centre already serves this need |
| Refund Officer | Refunds are a known fact after a cancellation; no decision to make |

## Scope by application

| Application | In the MVP |
|---|---|
| **Simulator** | Seeded service through the real ingestion path; delay, cancel and feed-stop scenarios |
| **Backend** | Journey state (lifecycle, punctuality, confidence), stale and unknown handling, source precedence, realtime updates, roles and audit |
| **Passenger app** | Public departure board and journey timeline, staleness labels, follow a journey |
| **Driver app** | Sign in, duty view, record departed and arrived at each stop, offline queue |
| **Ops console** | Live board; cancel, terminate, platform change with reason and audit; roles |
| **Platform display** | Live departures, honest when it cannot verify |

## Stretch (only after the core works)

Ticket purchase with QR and the Affected state, gate scan, fake payment provider.

## Deferred

Refund, Scheduler editing, Ticket Inspector, Station Master, station terminal, concourse display, reports, two languages, catering, reserved seats, rebooking.

## Build order

1. Live core: simulator, ingestion, journey state, passenger board, platform display
2. Driver app with offline queue
3. Controller board, then change actions with audit
4. Roles and admin
5. Stretch: tickets, payment, gate scan

If time runs short, cut from the bottom.

## Done when

1. The simulator runs Central to Riverside.
2. Passenger app, controller board and platform display show the same state and confidence.
3. A delay reaches all three within seconds; the scheduled time does not change.
4. The driver records a stop offline; it is replayed and reconciled.
5. The controller cancels a journey; every screen follows with an audit record.
6. The feed stops; screens turn Stale, then Unknown, and say so.

## Source

Requirements: `docs/00-requirements/06-requirements/requirements.md`. Roles: `mvp-roles.md` (needs updating to match this page).
