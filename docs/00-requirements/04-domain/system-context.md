# System context — how the applications connect

## The one idea

Five applications and an external system look like five products. They are not. They are **five views over one
thing: the current state of each journey.** Every app renders the same state with different density, for a
different person, in a different context of use.

What makes the system coherent is separating **who is allowed to assert a fact** from **who only reads it**.

## Writers and readers

> **MVP note (2026-10-06):** the incident row below is a *capability* of the Network Controller and the fleet row a capability of the Scheduler in the MVP; the Station Master is a role of its own (see [mvp-roles.md](../06-requirements/mvp-roles.md)). Authority is still divided by accountability; it is simply held by fewer accounts.

| Actor | Asserts | Authority | Typical latency | Application |
|---|---|---|---|---|
| **Scheduler** | Planned timetable, calendars, stopping patterns, vehicle and driver assignments | *What should happen* | Days ahead | Ops console |
| **Train detection feed** *(simulator)* | Train detected at / between locations | *Where it physically is* | Seconds | Backend ingestion API |
| **Driver** | Arrived, departed, skipped, delay reason, request assistance | *What happened at the stop, and why* | Per stop | Driver app |
| **Network controller** | Cancellation, termination, platform change, reassignment | *What changed in the plan, now* | Immediate | Ops console |
| **Incident manager** *(capability — Network controller)* | Incidents, severity, public messaging for serious events | *What is going on, officially* | Minutes | Ops console |
| **Station Master** *(own station only)* | Station-scoped facts: facilities, display assignment, local notices | *Local state at one station* | Minutes | Ops console (station-scoped) |
| **Fleet controller** *(capability — Scheduler)* | Vehicle unavailable from / to | *What can run* | Hours ahead | Ops console |
| **Gate scanner** *(device)* | Board and exit scan events | *Who passed which gate, and when* | Seconds | Kiosk gate mode |
| Passenger app | — | **read only** | follows | Flutter |
| Station display | — | **read only** | follows | React kiosk (display mode) |
| Station terminal | — | **read only** | follows | React kiosk (terminal mode) |
| Catering menu service | its own menu data only | **nothing** — SmartRail reads its menu; no railway data flows to it | follows | External (Node) |
| Customer service agent *(deferred)* | — | **read only** | follows | Ops console |

Two consequences fall straight out of this table:

1. **There is no "one admin".** Write authority is split by accountability. A scheduler must not cancel a running
   train; a controller must not rewrite next month's timetable; a station manager must not touch another station;
   a system administrator manages *access* and must not operate trains at all (REQ-OPS-001). The roles exist
   because the authority genuinely differs, not to satisfy a checklist.
2. **Readers never write railway truth.** The passenger app, displays, terminal and catering provider cannot change
   a journey. That keeps the attack surface on the public side small, and means a reader's failure — a dead display,
   a phone in a tunnel — can never corrupt the state everyone else sees.

## The picture

```
                       WRITERS                                         READERS
                                                                      
  Scheduler ──────────┐                                   ┌──────────▶ Passenger app (Flutter)
  (timetable, crews)  │                                   │
                      │                                   ├──────────▶ Station displays (React)
  Train detection ────┤      ┌──────────────────────┐     │
  feed (SIMULATOR)    ├─────▶│   SmartRail backend   │─────┼──────────▶ Station terminal (React)
                      │      │  ─────────────────    │     │
  Driver app ─────────┤      │  JOURNEY STATE        │     ├──────────▶ Ops console (all roles read)
  (stop events)       │      │  (one per journey)    │     │
                      │      │  + source precedence  │     └──────────▶ Catering provider (external)
  Controller ─────────┤      │  + audit              │                   reads ETAs, sends order events
  (cancel, platform)  │      └──────────────────────┘                   back about ITS OWN orders only
                      │                │
  Incident (NC) ──────┘                ├── Notifications (push) ──▶ followers of a journey
                                       └── Device registry ◀── heartbeats from displays/terminals
```

## The spine: one journey state

Every application is a projection of the same record. The record is described fully in
[state-models.md](state-models.md); in summary it carries three independent things:

| Dimension | Values | Question it answers |
|---|---|---|
| **Lifecycle** | Planned, Ready, Running, Completed, Cancelled, TerminatedShort, Interrupted, TerminatedEmergency | Is the train running at all? |
| **Punctuality** | signed delta in minutes + band (Early, OnTime, MinorDelay, MajorDelay) | How late is it? |
| **Data confidence** | Confirmed, Live, Scheduled, Unknown | How sure are we? |

Plus, per stop: scheduled / estimated / actual times, platform with its own state, and stop status.

Keeping these three dimensions separate is what lets the system say "cancelled, and it was running 12 minutes
late when it was cancelled", or "running, about 8 minutes late, but we have had no data for 2 minutes". A model
where "Delayed" is a lifecycle state cannot express either.

## How the applications differ in what they show

The same journey record, five renderings:

| Application | Density | Context of use | What it emphasises |
|---|---|---|---|
| Passenger app | Low | Walking, one hand, sunlight, seconds of attention | Next train, platform, delay as a delta, confidence |
| Station display — platform | Very low | Read from 5–10 m | Next 3 departures from *this* platform |
| Station display — concourse | Medium | Read from 5–10 m, scanned | All departures and arrivals, active disruptions |
| Station terminal | Low–medium | Standing, shared, touch, unfamiliar with the network | Search, routes, station facilities |
| Driver app | Low, glanceable | Cab: gloves, vibration, glare, **no typing while moving** | Next stop, running delay, messages |
| Ops console | High | Desk, keyboard, long sessions | Everything, filterable, with the actions their role permits |

## The product promise every connection must preserve

> **Never lie about a train.**

There are three ways to break it, and each has a counterpart in this architecture:

| Failure | Where it is handled |
|---|---|
| We don't know where the train is | Lifecycle unchanged, confidence → `Unknown`, ETAs revert to schedule (REQ-LIVE-004) |
| We know, but the information is old | Every client shows the age of what it shows (REQ-PAX-005, REQ-DSP-004) |
| We said something that turned out wrong | Retraction is a first-class operation that propagates like the original (REQ-PAX-009) |

## Why the simulator plays the infrastructure, not a phone

Real railways do not depend on drivers' phones for passenger-facing position. Position comes from **trackside
train detection** through the signalling system. We have no signalling system, so the simulator stands in for
that feed (ASM-008).

The consequence is deliberate and important:

> **No single client's connectivity can blind the platform.**

When the driver's phone goes dark in a tunnel, passengers keep receiving live information because the feed never
depended on that phone. The driver app is **supplementary** — it adds what only a person knows (why a train is
late, that a stop was skipped) and acts as a fallback when the feed is missing. See
[CF-03](../08-cross-app-flows/cf-03-driver-offline.md).

The simulator also publishes through **exactly the same authenticated ingestion API** a real feed would use
(REQ-SIM-001). A simulator that writes straight to the database would test a shortcut, not the system.

## Boundaries

| Boundary | Rule |
|---|---|
| SmartRail ↔ catering menu service | A separate service with its own seeded data. SmartRail reads the menu through an API key, caches it, and shows it with a call-to-order number. No railway data is sent to it and no orders pass through SmartRail. If it is down only the menu is unavailable (REQ-INT-004). |
| SmartRail ↔ train detection feed | Authenticated per source (REQ-SEC-004). Validated, de-duplicated, plausibility-checked (REQ-LIVE-005). |
| SmartRail ↔ safety systems | **None.** SmartRail is an information system. It never replaces signalling, radio or safety procedures (ASM-009). |
| Public ↔ operational | Public endpoints are read-only, anonymous, cached and rate-limited (REQ-PAX-001, REQ-SEC-002). |
| Station ops ↔ central ops | Same console, role-scoped. A station manager sees and edits only their own station (ASM-010). |

## What is out of this picture

- **Knock-on delay between trains.** A delayed train holding up the one behind it needs a track-capacity model. It
  is named here so it is not mistaken for an omission (REQ-LIVE-018).
- **Reserved seating and rebooking.** Documented only; see [ticketing](../12-ticketing/README.md). Basic ticketing is in scope.
- **Audio announcements.** A real station runs them in parallel; out of scope and noted as an accessibility limit.

## Ticketing in the picture (added 2026-10-06)

Ticketing adds one more kind of writer without breaking the reader rule:

| Actor | Asserts | Never |
|---|---|---|
| **Authenticated passenger** | Purchases and refund requests, on **their own** tickets | Anything about a journey's state |
| **Payment provider** (fake, or PayHere sandbox) | A payment or refund result, by signed callback | Ticket status directly — SmartRail decides |
| **Ticket inspector** | A scan, which records the result the server returned | A ticket's validity — the server decides |
| **Backend (automatic)** | A retry of a failed refund | Eligibility |

The link between the two worlds is the **journey id**. A ticket is a promise about a journey; the journey's state is the
railway's. When the journey changes (cancelled, terminated short), the railway state *causes* a ticket state change
([CF-10](../08-cross-app-flows/cf-10-affected-refund.md)); the reverse never happens. Inspection
([CF-11](../08-cross-app-flows/cf-11-inspection.md)) is the only place the two are read together.
