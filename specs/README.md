# Specs

SmartRail uses **Spec-Driven Development (SDD)**. Every non-trivial change is written down, reviewed and approved as a spec **before** any code is written, then verified against that same spec before it is closed.

```
Proposal ──▶ Review ──▶ Implementation ──▶ Verification ──▶ Archive
```

## When a spec is needed

| Change | Spec? |
|---|---|
| New feature, endpoint, screen, event, table or integration | **Yes** |
| Change to an existing API contract, data model, business rule or permission | **Yes** |
| Refactor that changes behaviour or module boundaries | **Yes** |
| Bug fix that changes agreed behaviour | **Yes** (small spec) |
| Bug fix that restores agreed behaviour | No, reference the spec it restores |
| Typos, comments, formatting, dependency bumps | No |
| Critical hotfix | No up front; write the spec within one day |

## Pre-spec evaluation

Answer these before writing a spec. Record the answers in the spec header.

1. **Is it trivial or a hotfix?** If yes, no spec (see table above).
2. **Which modules and apps does it touch?** Identity, Network, Journeys, Operations, Notifications, passenger app, driver app, console, kiosk, catering, simulator.
3. **What does it change?** API contract, events, data model, UI, permissions, observability.
4. **Is it small enough?** One spec must be deliverable in about three days. If not, split it.
5. **Does an approved spec already cover it?** Then update that spec instead of creating a new one.

## Lifecycle

| Status | Meaning | Who moves it |
|---|---|---|
| `Draft` | Being written, can change freely | Author |
| `In review` | Complete, waiting for approval | Author |
| `Approved` | Agreed. Implementation may start | Reviewer (owner) |
| `In progress` | Being implemented on a branch | Author |
| `Verifying` | Code merged to the branch, acceptance criteria being checked | Author |
| `Done` | All criteria pass, PR merged, moved to `archive/` | Author |
| `Rejected` / `Superseded` | Not going ahead, or replaced by another spec | Reviewer |

Rules:
- **No code before `Approved`.** Spikes to learn are allowed on a throw-away branch and are never merged.
- **Scope changes during implementation go back to the spec first.** Edit the spec, note it in its change log, and get the change approved.
- **One spec, one branch, one or more PRs.** Every PR names its spec.
- **Never leave a spec in limbo.** Close it as `Done`, `Rejected` or `Superseded`.

## Files and naming

```
specs/
├── README.md                       this guide and the register
├── TEMPLATE.md                     copy this to start a spec
├── SPEC-001-identity-foundation.md active specs live here
└── archive/                        Done, Rejected and Superseded specs
```

- File name: `SPEC-NNN-short-kebab-title.md`. Numbers are never reused.
- Branch name: `feature/<feature-name>` (or `fix/<name>` for bug fixes). Name the branch after the feature, not the spec number. A spec may use several branches.
- Commit subject: conventional commit, with the spec ID in the body, for example:

```
feat(journeys): record stop events from the driver app

Refs: SPEC-003
```

## Traceability

Each spec links **upwards** to requirements (`REQ-…`, `UC-…`, `US-…`, `CF-…`, `D-…`, `OQ-…`, `ADR-…`, Figma frames) and **downwards** to code, tests and PRs. Acceptance criteria are numbered (`AC-1`, `AC-2` …) and each one maps to at least one automated test. After a spec is `Done`, run:

```bash
python tools/docs/build_traceability.py
```

## Register

Dependency order. Each spec is about three days or less; split anything bigger.

| ID | Title | Status | Modules / apps | Branch |
|---|---|---|---|---|
| SPEC-001 | Identity foundation (Keycloak, roles, secure by default) | Built; spec to be written retrospectively | Identity, API | `feature/authentication` |
| SPEC-002 | Network reference data and seed | Verifying | Network | `feature/network-reference-data` |
| SPEC-003 | Timetable: versions, calendars, services, public timetable | Verifying | Network | `feature/timetable` |
| SPEC-004 | Journey generation and the journey model (lifecycle, per-stop state, assignments, scheduled board) | In progress | Journeys | `feature/journeys` |
| SPEC-005 | Live feed ingest, delay, confidence (Live, Stale, Unknown), source precedence | Planned | Journeys | — |
| SPEC-006 | Simulator scenarios through the real ingest endpoint | Planned | Simulator | — |
| SPEC-007 | Departure board and journey timeline API, realtime push | Planned | Journeys, Notifications | — |
| SPEC-008 | Flutter foundation: shared packages, app shell, router, localization | Planned | passenger app, driver app | — |
| SPEC-009 | Passenger app: departures board and journey timeline with staleness | Planned | passenger app | — |
| SPEC-010 | Controller actions with audit (cancel, terminate, platform change) and live board | Planned | Operations, console | — |
| SPEC-011 | Driver app: duty, stop events, offline queue and replay | Planned | Journeys, driver app | — |
| SPEC-012 | Assignments (vehicle, driver), vehicle unavailability and roles administration | Planned | Journeys, Identity, console | — |
| SPEC-013 | Platform display (kiosk) | Planned | kiosk | — |
| SPEC-014 | Station search and route stops in the passenger app | Planned | passenger app | — |
| SPEC-015 | Timetable versioning workflow: draft, review, approve, publish (API and console) | Planned | Network, console | — |
| SPEC-016 | Journey search A to B with one transfer (REQ-PAX-006) | Planned | Network, Journeys, passenger app | — |
| SPEC-017 | Public rate limiting (REQ-SEC-002) and permission-matrix tests (REQ-SEC-003) | Planned | API, all modules | — |
| SPEC-018 | Web foundation: shared packages, design tokens, auth, routing, data layer (console and kiosk) | Planned | console, kiosk | — |
| SPEC-019 | Station terminal: destination search, station information, idle reset, lockdown, offline mode | Planned | kiosk | — |

Stretch, only after the core works: tickets, payment, gate scan.

## When each app starts

Apps start as soon as the data they show exists. The foundations do not depend on the backend and start now.

| Phase | Backend | Mobile (Flutter) | Web (React) | Kiosk and displays |
|---|---|---|---|---|
| **A. Now** | SPEC-003 timetable (done), SPEC-004 journeys | **SPEC-008 Flutter foundation** (packages, theme from Figma, router, localization) | **SPEC-018 Web foundation** (tokens, auth, routing, data layer) | Uses the web foundation |
| **B. First screens** | SPEC-004 gives scheduled journeys and a station board | **SPEC-009 passenger app: departures board and journey timeline** (scheduled first) | | |
| **C. Live core** | SPEC-005 ingest and confidence, SPEC-006 simulator, SPEC-007 realtime and live board | SPEC-009 turns live: delay, Stale, Unknown | **SPEC-010 controller actions and live board** (first console screens) | **SPEC-013 platform display** (first kiosk screen) |
| **D. Drivers and operations** | SPEC-011 duty and stop events, SPEC-012 assignments and roles | **SPEC-011 driver app**: duty, one-tap events, offline queue | Console: assignments, roles, devices | Concourse display, **SPEC-019 station terminal** |
| **E. Passenger depth** | SPEC-014 station search, SPEC-016 journey search, notifications | SPEC-014, SPEC-016, follow and notifications | | |
| **F. Tickets and gates (MVP)** | Ticketing specs, SPEC-015 timetable workflow, SPEC-017 hardening | Tickets, QR wallet, inspector mode | Timetable versioning screens | Gate mode |

The first UI to be built is the **Flutter foundation**, in parallel with SPEC-004, and the first real screen is the **passenger departures board**, because SPEC-004 already serves it scheduled data.

Update this table whenever a spec changes status.
