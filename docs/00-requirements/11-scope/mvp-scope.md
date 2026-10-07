# MVP scope

Brief §25: explore the broader problem, choose an achievable scope, prioritise, document what is deferred and why,
and build the chosen scope to quality. *A smaller, coherent, well-engineered solution is preferable to a large
collection of incomplete features* — without collapsing into a single application.

> **Updated 2026-10-06 (owner decisions):** ticketing, accounts and payments are **in** as a thin, gated slice;
> catering is **out** entirely. Detail and the 35 ticketing decisions: [12-ticketing/README.md](../12-ticketing/README.md).

## The primary deliverables

| # | Deliverable | Technology | Notes |
|---|---|---|---|
| 1 | **Backend** | .NET, clean architecture, modular monolith | Live journey state, ingestion, disruptions, ticketing, payments port, realtime, audit, identity |
| 2 | **Operations web console** (internal system) | React | All staff roles, role-scoped; station operations is a scoped view of this console (ASM-010) |
| 3 | **Passenger app** | Flutter, feature-based clean architecture | Boards, journeys, following, accounts, tickets and QR wallet |
| 4 | **Driver / staff app** | Flutter, same codebase family | Driver duty and stop events, **plus Ticket Inspector mode** (QR scan) |
| 5 | **Station kiosk** | React, one codebase, two modes | Platform and concourse displays; interactive terminal |
| 6 | **Journey simulator** | .NET worker | The source of all live data; plays the train detection feed. Not optional |

Not deliverables: a catering platform, a separate station-operations application, a separate inspector app.

## Roles in the MVP

Six roles have accounts: **Passenger, Driver, Ticket Inspector, Network Controller, Scheduler, System Administrator**. Station Manager, Incident Manager, Fleet Controller and Reporting Analyst are capabilities held by the Network Controller or Scheduler; Customer Service is deferred. Why, and what it costs: [mvp-roles.md](../06-requirements/mvp-roles.md).

## The scoping principle

> Build the **moment of change** end to end — something changes during a running service, and every application has a
> part in it — **and the main passenger flow around it**: buy a ticket, the train is cancelled, the ticket is affected,
> the passenger is refunded, an inspector rejects the stale ticket.

## Build levels

| Level | Meaning |
|---|---|
| **Yes** | Built fully, tested, demoed |
| **Thin** | Built minimally — enough to demonstrate the mechanism honestly |
| **No** | Documented and designed; not built |

## By deliverable

| Deliverable | Yes | Thin | No (documented) |
|---|---|---|---|
| **Backend** | Journey lifecycle and three-dimension state; ingestion with validation; source precedence; ETA propagation; journey generation; service date; time-ranged assignments; platform state; disruptions with scope resolution; realtime snapshot and delta; audit; permissions; device identity; accounts; tickets with signed QR; payment port with fake provider and idempotent signed callbacks; refunds | Rotation cascade; platform conflict warning; account deletion | Knock-on delay between trains; reserved seat inventory |
| **Ops console** | Live board; cancel / terminate / reinstate; platform change; disruption console with preview, approval, retraction; timetable versioning; unassigned queue; device health; users and roles; punctuality report | Incidents and emergency termination; vehicle unavailability; driver messages; data correction; planned works; read-only refund view | Customer-service lookup; break-glass access; shift-pattern rostering |
| **Passenger app** | Station board; journey timeline; staleness labels; follow, throttled push and inbox; disruption banners and corrections; offline cache; delete data; screen reader and text scaling; **sign-in; buy a ticket; QR wallet (offline); affected-ticket notice; refund** | — | Map view; SMS and email |
| **Driver / staff app** | Duty view offline; one-tap stop events; reason codes; offline queue and replay; message acknowledgement; request assistance; **Ticket Inspector mode: scan, validate, scan log** | Crew handover | Guard role beyond inspection; location collection beyond duty-scoped |
| **Station kiosk** | Platform and concourse displays; device token; offline honesty; heartbeat; server-time countdowns; terminal search, station info, disruptions, idle reset, lockdown, offline mode | — | Audio announcements; signage CMS; ticket purchase at the terminal |
| **Simulator** | Real-ingestion path; seeded scenarios; acceleration; fault injection; **fake payment provider with fault switches**; scenario library SIM-01 … 20 | — | Real signalling integration |

## Cut or deferred — and why

| Item | Decision | Reason |
|---|---|---|
| Separate Station Manager, Incident Manager, Fleet and Reporting roles; Customer Service | Folded into the Network Controller and Scheduler, or deferred | Roles are permission bundles; the demo needs the capabilities, not extra accounts (ASM-045) |
| **Catering platform and ordering** | **Cut entirely** (ASM-005) | Owner priority; ticketing is the main flow. Forgoes the Integration row of §26 and one §23 artifact — knowingly |
| Reserved seats, Intercity / High-speed sales, seat holds | Not built | Needs inventory and concurrency control; Regional unreserved is enough to demonstrate the flow |
| Rebooking | Not built | Rules documented; refund is the MVP outcome |
| Manual refund approval queue and refund staff role | Replaced by auto-approval and automatic retry | Eligibility after a railway cancellation is a fact; recovery needs no person |
| PayHere sandbox | Stretch | Needs the owner's keys; fake provider keeps the build independent |
| Fare adjustment for travelling past a destination | Out | Inspection records it; the fine is the railway's process |
| Knock-on delay between trains | Out | Needs a track-capacity model; named so it is not mistaken for an omission |
| Train formation, splitting, joining | Out | Rare and hard (ASM-015) |
| Map view | Out | The schematic timeline answers the question, works offline (ASM-031) |
| SMS and email | Out | Push plus inbox covers the demonstrated journeys |
| Audio announcements | Out | A separate system in real stations; noted as an accessibility limit |
| Machine-learning ETA | Out | Unexplainable under live review (§28) |
| Kubernetes, multi-region | Out | Documented production path |

## Pending decisions that could change this page

| Decision | Default here | If changed |
|---|---|---|
| OQ-02 position source | Simulator as infrastructure feed | Driver-GPS-primary moves CF-03 into the critical path |
| OQ-03 station operations | Role-scoped console view | A separate app adds ~2 days |
| OQ-04 station experience | Both modes | Dropping one saves ~1 day |
| OQ-17 PayHere keys | Fake provider only | Adds the stretch adapter |

## The gate on ticketing

The ticketing slice (~4–5 days) is built **after the live core works end to end (E2E-1)**. At the D14 feature
freeze anything unfinished is cut to documented-only with the reason recorded. Build order if time is short:

1. accounts, purchase against the fake provider, QR ticket — the demo spine
2. inspector validation
3. cancellation → Affected → refund
4. refund Retry view

## The five E2E flows (brief §23)

| # | Flow | Cross-app flow | Why it is critical |
|---|---|---|---|
| E2E-1 | Passenger follows a delayed train | [CF-01](../08-cross-app-flows/cf-01-delay-propagation.md) | The core value proposition; crosses every layer |
| E2E-2 | Driver records offline and it reconciles | [CF-03](../08-cross-app-flows/cf-03-driver-offline.md) | The highest-risk mechanism (§16) |
| E2E-3 | Controller cancels a running journey; every surface follows | [CF-02](../08-cross-app-flows/cf-02-cancellation.md) | Highest-consequence action; authorisation, audit, fan-out |
| E2E-4 | **Buy a ticket → journey cancelled → ticket Affected → refund → inspector rejects it** | [CF-09](../08-cross-app-flows/cf-09-purchase.md), [CF-10](../08-cross-app-flows/cf-10-affected-refund.md), [CF-11](../08-cross-app-flows/cf-11-inspection.md) | The main passenger flow; payment idempotency; the railway and the ticket staying consistent |
| E2E-5 | Display survives connectivity loss honestly | [CF-06](../08-cross-app-flows/cf-06-degraded-data.md) | "Never lie about a train" under failure |

*E2E-4 replaced the catering flow when catering was cut.*
