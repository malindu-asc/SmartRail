# 01 — Problem framing

## 1.1 What the organisation told us

A national railway operator runs passenger services across a network of stations. Thousands of passengers use
them daily. It wants a digital platform, **SmartRail**, to:

- give passengers accurate information about services, routes, stations, upcoming trains, expected times,
  current train locations *where appropriate*, and disruptions (brief §2, §3);
- give drivers, station staff and central operations the tools to run and oversee the service (§8, §9);
- put information on station screens and interactive terminals (§6, §7);
- coordinate with an external catering provider so passengers can order food for collection during a journey
  (§11, §12).

It expects several applications, not one (§2).

## 1.2 What it is really trying to fix

Read as a whole, the brief describes one underlying problem with several symptoms:

> **The railway's operational reality and what people are told about it drift apart — and the further they
> drift, the worse every decision made downstream becomes.**

A passenger chooses a train using a departure time that has already changed. A station screen shows a train
that has been cancelled. A controller does not know a driver never received an instruction. A catering order is
prepared for a train that is twenty minutes late. Each is a failure of the *same* thing: a single, honest,
current picture of the railway, delivered to the right person in the form they can use.

That framing matters because it sets the product's first principle:

> **Never lie about a train.** When we know, say it. When the information is old, say how old. When we do not
> know, say that — never fill the gap with a confident guess.

Almost every edge case in this project is a way of breaking that principle.

## 1.3 Initial problem areas

These are hypotheses taken from the brief. They are refined in discovery, not treated as requirements.

| Area | The problem as the brief implies it | Brief |
|---|---|---|
| Passenger information | Passengers cannot rely on timely, understandable journey information | §3 |
| Live journey visibility | Passengers cannot see where a train is or when it will arrive | §5 |
| Operational visibility | Operations staff lack a network-wide current picture | §9 |
| Disruption handling | Changes are not managed or communicated consistently | §10 |
| Station information | Stations need current information on screens and terminals | §6, §7 |
| Driver operations | Drivers need journey-specific operational information | §8 |
| Catering coordination | An external provider needs journey information to fulfil orders | §11, §12 |
| Access control | Different users need different permissions — not one administrator | §9, §14 |
| Connectivity | Trains, terminals and phones lose connection | §16 |
| Security and privacy | Passenger and operational data need protection | §17, §20 |

## 1.4 What the brief deliberately does not say

The brief is explicit that it is incomplete and that finding the gaps is part of the assessment (§22). The
largest unknowns, each picked up in discovery:

| Unknown | Why it matters | Where it is resolved |
|---|---|---|
| Where live train position actually comes from | Everything "live" depends on it | [system-context.md](04-domain/system-context.md), ASM-008 |
| What "live", "accurate" and "upcoming" mean numerically | Nothing is testable without numbers | REQ-LIVE-004, ASM-019, ASM-020 |
| The lifecycle of a journey, a disruption, a device | Undefined states produce contradictory screens | [state-models.md](04-domain/state-models.md) |
| What a "train" is — service, journey, vehicle, crew | Wrong identity breaks tracking on substitution | [identity-model.md](04-domain/identity-model.md) |
| Who may change what | The brief rejects one admin role but defines none | [permission-matrix.md](06-requirements/permission-matrix.md) |
| What happens when things fail | §16 raises it; nothing specifies behaviour | [degraded-mode-matrix.md](06-requirements/degraded-mode-matrix.md) |
| The catering workflow | §12 lists eleven open questions itself | [external-catering.md](07-use-cases/external-catering.md) |
| The current process | Not described anywhere | [03-current-state.md](03-current-state.md) |

## 1.5 Problem statement

> Passengers, station staff, drivers and operations staff each act on information about the railway that may be
> late, inconsistent or silently out of date, and none of them can tell when it is. SmartRail must give each of
> them a single, current, honest view of train running — one that states its own freshness and degrades
> visibly rather than silently when connectivity or data fails — while keeping the authority to change that
> information with the people accountable for it.

## 1.6 What success looks like (for this assessment)

| Outcome | Evidence |
|---|---|
| A passenger can tell in three seconds whether their train is running, from where, and how sure the system is | E2E flow 1; REQ-PAX-002, REQ-PAX-004, REQ-PAX-005 |
| A disruption declared once reaches every surface consistently, and can be retracted the same way | E2E flow 3; CF-04 |
| Losing connectivity on any single client never blinds anyone else | E2E flows 2 and 5; CF-03, CF-06 |
| Every high-consequence action is attributable to a person with the authority to take it | REQ-OPS-001, REQ-OPS-011 |
| The external provider can fail without the railway failing | E2E flow 4; REQ-INT-004 |
