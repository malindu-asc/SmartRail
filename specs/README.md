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

| ID | Title | Status | Modules / apps | Branch |
|---|---|---|---|---|
| SPEC-001 | Identity foundation (Keycloak, roles, secure by default) | Planned (built before SDD; spec to be written retrospectively) | Identity, API | `feature/authentication` |
| SPEC-002 | Network reference data and seed | Verifying | Network | `feature/network-reference-data` |
| SPEC-003 | Ingest stop events and journey state with confidence | Planned | Journeys, simulator | — |
| SPEC-004 | Live departure board and realtime push | Planned | Journeys, Notifications, passenger app, kiosk | — |
| SPEC-005 | Driver duty, stop reporting and offline queue | Planned | Journeys, driver app | — |
| SPEC-006 | Controller actions with audit (cancel, terminate, platform change) | Planned | Operations, console | — |
| SPEC-007 | Routes, rosters and driver and unit assignment | Planned | Network, console | — |
| SPEC-008 | Admin: users, roles and devices | Planned | Identity, console | — |
| SPEC-009 | Station displays and terminal | Planned | kiosk | — |

Update this table whenever a spec changes status.
