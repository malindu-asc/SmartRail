# MVP roles

Discovery found eleven roles (see [stakeholders and impact](../02-stakeholders-and-impact.md) and the
[permission matrix](permission-matrix.md)). The MVP **builds seven**. The rest are kept as *capability bundles* — sets of
permissions held by one of the seven — or deferred. Nothing found in discovery is deleted; it is assigned.

> **Decisions (Malindu):** 2026-10-06 — do not create roles the MVP does not need (ASM-045). **2026-10-07 — the Station
> Master is restored as a role**, because gate scanners and station-level work created real station-scoped duties and the
> Network Controller was carrying too much.

## Why this is sound

- **Roles are bundles of permissions.** Restoring or merging a role is a configuration change, not a redesign.
- **Authority is divided by accountability.** The administrator cannot operate trains; the scheduler cannot cancel a running
  train; the controller cannot edit timetables or manage users; **the Station Master can act only on their own station.**
- **Fewer accounts and tests**, and a scoped role that makes the RBAC demo concrete.

## The seven roles

| Role | Where it works | Depth | What it can do | What it cannot do |
|---|---|---|---|---|
| **Passenger** | Passenger app, station terminal | Full | Anonymous: boards, journeys, search, follow, disruptions. Signed in: buy, hold, refund **own** tickets | See anyone else's tickets; change any journey |
| **Driver** | Staff app | Full | See **own** duty; record arrived / departed / skipped, delay reason, request assistance; acknowledge messages; work offline | Cancel a journey; edit the timetable; read another driver's duty |
| **Ticket Inspector** | Staff app (a mode, on the inspector's own phone) | Core | Spot-check passengers by scanning a QR; see the result and reason | Do driver actions unless also a Driver |
| **Network Controller** | Ops console | Full | Live board; cancel, terminate short, reinstate; change platforms; publish, update, retract disruptions (second approver for network-wide); declare an incident and emergency termination; message drivers; read all device health and scan logs; punctuality report; read-only refund view | Edit timetables; assign vehicles or drivers; manage users |
| **Station Master** | Ops console, **own station only** | Core | See own station's arrivals and departures; see own displays and gate scanners (online or not); update lift, stair and gate status; post local station notices; see the gate scan log and the excess-fare list; mark an excess fare collected; see platform changes the moment they happen | Cancel trains; publish disruptions; change platforms; edit timetables; manage users; touch **any other station** |
| **Scheduler** | Ops console | Core | Draft timetables (another person approves); assign vehicles and drivers; mark a vehicle unavailable; schedule planned works | Cancel a running journey; approve own timetable |
| **System Administrator** | Ops console | Core | Create users, assign roles, provision and revoke devices (displays, terminals, gate scanners) | Any operational action |

**Demo accounts:** Passenger, Driver, Ticket Inspector, **two** Network Controllers (the second approves network-wide alerts),
**two** Station Masters at different stations (for the scope test), Scheduler, System Administrator.

## The roles that are not separate accounts — and why

| Discovered role | In the MVP | Why not a separate account | What we give up | In production |
|---|---|---|---|---|
| **Incident Manager** | Capability of the Network Controller: declare an incident and emergency termination behind a confirmation step, a reason and an audit entry (ASM-047) | One thin serious-incident action is enough for the demo | "Only one role can declare an emergency" | A dedicated role with a handover process |
| **Reporting Analyst** | Capability of the Network Controller: punctuality report and export | Read-only; a capability, not a job | Nothing material | A reporting role |
| **Fleet Controller** | Capability of the Scheduler: mark a vehicle unavailable | Vehicle availability and assignment are the same planning task | Nothing material | A fleet role if maintenance is run separately |
| **Customer Service Agent** | **Deferred** — the call center handles customer information outside the system | Passenger self-service covers the main flow; failed refunds retry automatically (ASM-046) | A person to answer questions and recover refunds | A support role with verified-caller lookup and a case log |

## Risks of this choice, and the controls

| Risk | Control |
|---|---|
| The Network Controller holds many capabilities | Audit on every high-consequence action; a **second approver** for network-wide alerts; a confirmation step and reason for emergency termination |
| One person can declare an emergency alone | Confirmation, reason and audit; production splits the role (ASM-047) |
| A failed refund has no human owner | Automatic retry with backoff; the passenger can request again (idempotent); the Network Controller sees refunds read-only (ASM-046) |
| Station scope must actually hold | Negative tests: a Station Master at one station cannot read or edit another's notices, lift status, scan log or devices |

## What the seven roles can do together

`✓` allowed · `own` own records only · `✗` denied

| Capability | Passenger | Driver | Inspector | Net. Controller | Station Master | Scheduler | Sys. Admin |
|---|---|---|---|---|---|---|---|
| Browse boards, journeys, disruptions | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Follow a journey | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| Buy, hold and refund tickets | own | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| View own duty, record stop events, request assistance | ✗ | own | ✗ | ✗ | ✗ | ✗ | ✗ |
| Spot-check a ticket QR | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| Live network board | ✗ | ✗ | ✗ | ✓ | own station | read | ✗ |
| Cancel / terminate / reinstate; change a platform | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ |
| Publish, update, retract disruptions; approve network-wide | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ |
| Declare incident / emergency termination | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ |
| Message drivers | ✗ | acknowledge | ✗ | ✓ | ✗ | ✗ | ✗ |
| Device health | ✗ | ✗ | ✗ | all | own station | ✗ | all |
| Gate scan log; excess-fare list | ✗ | ✗ | own scans | all | own station | ✗ | ✗ |
| Mark an excess fare collected | ✗ | ✗ | ✗ | ✗ | own station | ✗ | ✗ |
| Lift / stair / gate status; local station notices | ✗ | ✗ | ✗ | ✗ | own station | ✗ | ✗ |
| Punctuality report and export | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ |
| Read refunds (read-only) | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ |
| Draft timetables; approve another person's | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ | ✗ |
| Assign vehicles and drivers; mark a vehicle unavailable; planned works | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ | ✗ |
| Create users, assign roles, provision and revoke devices | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ |
| Any operational action | ✗ | ✗ | ✗ | per above | per above | per above | **✗** |

## Negative tests that matter in the MVP

- A Scheduler, Driver, Station Master or Administrator declares a Critical incident → 403.
- A Driver, Station Master or Scheduler changes a platform → 403.
- A Network Controller edits a timetable or creates a user → 403.
- The Scheduler cancels a running journey → 403; the author approves their own timetable → rejected.
- The Administrator cancels, publishes, refunds or inspects → 403.
- **A Station Master at one station edits another station's notices, lift status, scan log or devices → 403.**
- A Station Master cancels a journey or publishes a disruption → 403.
- A passenger reads or refunds **another** passenger's ticket → 403 (IDOR).
- A driver reads **another** driver's duty → 403 (IDOR).
- A driver token validates a ticket → 403; an anonymous call to any ticket endpoint → 401.
- A gate device token reads a ticket or any journey beyond its scan result → 403.

## Non-human actors

| Actor | What it does | Cannot |
|---|---|---|
| **Display / terminal device** | Shows information, heartbeat every 30 s, revocable token | Write anything |
| **Gate scanner (kiosk gate mode)** | Sends board and exit scans to the validation endpoint; shows the result; station-scoped token; heartbeat | Decide anything itself; read tickets |
| **Train detection feed (simulator)** | Reports train position and stop events with its own key | Read anything back |
| **Payment provider (fake)** | Hosts checkout; signed confirmations and refund results | Change a ticket directly |
| **Push service** | Delivers notifications | Be the source of truth |
| **Background jobs** | Journey generation, order expiry, no-vehicle alerts, notification throttling, retention purge, closing boarded tickets | Act outside their schedule |

## If a role is restored or merged

Create or merge the role, move its permissions between bundles, add its demo user and negative tests. No domain or API change.
