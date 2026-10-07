# MVP roles

Discovery found eleven roles (see [stakeholders and impact](../02-stakeholders-and-impact.md) and the
[permission matrix](permission-matrix.md)). The MVP **builds six**. The rest are kept as *capability bundles* —
sets of permissions held by one of the six — or deferred. Nothing found in discovery is deleted; it is assigned.

> **Decision (Malindu, 2026-10-06):** do not create roles that the MVP does not need. Use the roles and their
> capabilities, and justify every role that is not built (ASM-045).

## Why this is sound

- **Roles are bundles of permissions.** Restoring a dropped role later is a configuration change, not a redesign.
- **Authority is still divided by accountability.** The administrator cannot operate trains; the scheduler cannot
  cancel a running train; the controller cannot edit timetables or manage users. That separation, which is what
  brief §9 actually asks for, is untouched.
- **Fewer roles means fewer accounts, screens and negative tests** — time that goes to the demo flow instead.

## The six roles

| Role | Where it works | Depth | What it can do | What it cannot do |
|---|---|---|---|---|
| **Passenger** | Passenger app, station terminal | Full | Anonymous: boards, journeys, search, follow, disruptions. Signed in: buy, hold and refund **own** tickets | See anyone else's tickets; change any journey |
| **Driver** | Driver app | Full | See **own** duty; record arrived / departed / skipped, delay reason, request assistance; acknowledge messages; work offline | Cancel a journey; edit the timetable; read another driver's duty |
| **Ticket Inspector** | Driver app (a mode) | Core | Scan a ticket QR and see the result with its reason | Do driver actions unless also assigned the Driver role |
| **Network Controller** | Ops console | Full | Live board; cancel, terminate short, reinstate; change platforms; publish, update, retract disruptions (second approver for network-wide); declare an incident and emergency termination; message drivers; edit station info and see device health; punctuality report; read-only refund view | Edit timetables; assign vehicles or drivers; manage users |
| **Scheduler** | Ops console | Core | Draft timetables (another person approves); assign vehicles and drivers; mark a vehicle unavailable; schedule planned works | Cancel a running journey; approve own timetable |
| **System Administrator** | Ops console | Core | Create users, assign roles, provision and revoke devices | Any operational action |

**Demo accounts:** Passenger, Driver, Ticket Inspector, **two** Network Controllers (the second is the approver for
network-wide alerts), Scheduler, System Administrator.

## The roles that are not built — and why

| Discovered role | In the MVP | Why not a separate account | What we give up | In production |
|---|---|---|---|---|
| **Station Manager** | Capability of the Network Controller: edit station info, a station view of the board and device health | A station view in the same console covers the demo; a separate scoped role adds an account and tests for no new flow | The "own station only" staff scope. Scoped RBAC is still demonstrated by Driver (own duty) and Passenger (own tickets) | A station-scoped role |
| **Incident Manager** | Capability of the Network Controller: declare an incident and emergency termination behind a confirmation step, a reason and an audit entry (ASM-047) | One thin serious-incident action is enough for the demo | "Only one role can declare an emergency" | A dedicated role with a handover process |
| **Customer Service Agent** | **Deferred** | Passenger self-service covers the main flow; failed refunds retry automatically (ASM-046) | A person to answer questions and recover refunds | A support role with read-only lookup |
| **Fleet Controller** | Capability of the Scheduler: mark a vehicle unavailable | Vehicle availability and assignment are the same planning task | Nothing material | A fleet role if maintenance is run separately |
| **Reporting Analyst** | Capability of the Network Controller: punctuality report and export | Read-only; a capability, not a job | Nothing material | A reporting role |

## Risks of this choice, and the controls

| Risk | Control |
|---|---|
| The Network Controller holds many capabilities | Audit entry on every high-consequence action; a **second approver** for network-wide alerts; a confirmation step and reason for emergency termination |
| One person can declare an emergency alone | Confirmation, reason and audit; production splits the role (ASM-047) |
| No staff scope to demonstrate | Negative tests use Driver `own` duty and Passenger `own` tickets |
| A failed refund has no human owner | Automatic retry with backoff; the passenger can request again (idempotent); the Network Controller sees refunds read-only (ASM-046) |

## What the six roles can do together

`✓` allowed · `own` own records only · `✗` denied

| Capability | Passenger | Driver | Inspector | Net. Controller | Scheduler | Sys. Admin |
|---|---|---|---|---|---|---|
| Browse boards, journeys, disruptions | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ |
| Follow a journey | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ |
| Buy, hold and refund tickets | own | ✗ | ✗ | ✗ | ✗ | ✗ |
| View own duty, record stop events, delay reason, request assistance | ✗ | own | ✗ | ✗ | ✗ | ✗ |
| Validate a ticket QR | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ |
| Live network board | ✗ | ✗ | ✗ | ✓ | read | ✗ |
| Cancel / terminate / reinstate a journey; change a platform | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ |
| Publish, update, retract disruptions; approve network-wide | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ |
| Planned engineering works | ✗ | ✗ | ✗ | ✗ | ✓ | ✗ |
| Declare incident / emergency termination | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ |
| Message drivers | ✗ | acknowledge | ✗ | ✓ | ✗ | ✗ |
| Station info; device health | ✗ | ✗ | ✗ | ✓ | ✗ | health only |
| Punctuality report and export | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ |
| Read refunds (read-only) | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ |
| Draft timetables; approve another person's | ✗ | ✗ | ✗ | ✗ | ✓ | ✗ |
| Assign vehicles and drivers; mark a vehicle unavailable | ✗ | ✗ | ✗ | ✗ | ✓ | ✗ |
| Create users, assign roles, provision and revoke devices | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ |
| Any operational action | ✗ | ✗ | ✗ | per above | per above | **✗** |

## Negative tests that matter in the MVP

- A Scheduler, Driver or Administrator token declares a Critical incident → 403.
- A Driver or Scheduler token changes a platform → 403.
- A Network Controller edits a timetable or creates a user → 403.
- The Scheduler cancels a running journey → 403; the author approves their own timetable → rejected.
- The Administrator cancels, publishes, refunds or inspects → 403.
- A passenger reads or refunds **another** passenger's ticket → 403 (IDOR).
- A driver reads **another** driver's duty → 403 (IDOR).
- A driver token validates a ticket → 403; an anonymous call to any ticket endpoint → 401.

## Non-human actors

| Actor | What it does | Cannot |
|---|---|---|
| **Display / terminal device** | Shows information, sends a heartbeat every 30 s, revocable token | Write anything |
| **Train detection feed (simulator)** | Reports train position and stop events with its own key | Read anything back |
| **Payment provider (fake)** | Hosts checkout; sends signed confirmations and refund results | Change a ticket directly — SmartRail decides |
| **Push service** | Delivers notifications | Be the source of truth — every push is also in the inbox |
| **Background jobs** | Journey generation, order expiry, no-vehicle alerts, notification throttling, retention purge | Act outside their schedule |

## If a role is restored

Create the role, move its permissions out of the Network Controller or Scheduler bundle, add its demo user and its
negative tests. No domain or API change is needed.
