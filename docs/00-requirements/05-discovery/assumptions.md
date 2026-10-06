# Assumptions, facts and decisions

> Seeded 2026-10-05 from the discovery work. **This file is now the source of truth — edit it directly.**
> Every seeded row starts as `Proposed` (AI-drafted). Review each one and change it to `Accepted`, `Changed` or `Rejected`. The count of reviewed rows is evidence for brief §21.

Three different kinds of statement are kept apart on purpose. Mixing them is the most common way an assumption register goes wrong:

| Type | Meaning | Where its justification lives |
|---|---|---|
| **Fact in brief** | The brief states it. Not an assumption at all. | The brief section cited |
| **Decision (user)** | Malindu decided it. | This register |
| **Decision (me)** | A design choice made deliberately, with a rejected alternative. | An ADR |
| **Assumption** | Something unknown that we had to fill in to proceed. | This register — with the risk if wrong |

Statuses are chosen so every row can be **closed**. There is no client to validate with, so `To validate` would still be open on submission day.

`Confirmed by brief` · `Decided by user` · `Decided by me` · `Assumed - pending confirmation` · `Question sent` · `Closed`

> **Pending confirmation from Malindu:** every row with status `Assumed - pending confirmation`. The four that change the most work are ASM-008 (simulator = infrastructure feed), ASM-010 (station operations is a role-scoped console view), ASM-011 (both displays and terminal) and ASM-012 (no passenger accounts).

## Fact in brief

| ID | Statement | Why needed | Risk if wrong | Impact | Status | Used by |
|---|---|---|---|---|---|---|
| ASM-001 | The catering provider is a separate external system and may be mocked, simulated or built as a small independent application. | Defines the integration boundary. | None - stated in brief 12 and 23. | Low | Confirmed by brief | — |
| ASM-002 | Operational users must not be treated as a single Administrator role. | Basis of the permission model. | None - stated in brief 9. | Low | Confirmed by brief | — |
| ASM-003 | Five end-to-end flows must be automated. | Fixes QA scope. | None - stated in brief 23. | Low | Confirmed by brief | — |
| ASM-004 | Trains, terminals and passengers may have unreliable connectivity. | Drives the whole degraded-mode design. | None - stated in brief 16. | Low | Confirmed by brief | — |

## Decision (user)

| ID | Statement | Why needed | Risk if wrong | Impact | Status | Used by |
|---|---|---|---|---|---|---|
| ASM-005 | Catering integration is low priority. It is built thin and last. | User instruction. Keeps scoring on the graded Integration row at minimum cost. | If dropped entirely the Integration row in brief 26 and a 23 artifact are forfeited. | Medium | Decided by user | — |
| ASM-006 | There is no single all-powerful administrator; roles are divided by accountability. | User instruction, consistent with brief 9. | None. | Low | Decided by user | REQ-OPS-001 |
| ASM-007 | Backend is .NET clean architecture, web is React, mobile is Flutter feature-based clean architecture. | User instruction. | None. | Low | Decided by user | — |

## Decision (me)

| ID | Statement | Why needed | Risk if wrong | Impact | Status | Used by |
|---|---|---|---|---|---|---|
| ASM-008 | The simulator represents the infrastructure / train-detection feed (signalling), not a GPS device. The driver app is supplementary. | Real railways derive position from trackside detection. It also means no single client's connectivity can blind passengers. | If the assessor expects device GPS, the narrative changes but the architecture still works (add a second source). | High | Assumed - pending confirmation | ADR-0006 |
| ASM-009 | SmartRail is an information system. It is not a safety-critical system and never replaces signalling, radio or safety procedures. | Prevents the design implying safety guarantees it cannot give, especially for crash handling. | If treated as safety-critical the entire assurance and certification burden changes. | High | Decided by me | — |
| ASM-013 | Ten roles exist in the permission model; six (NetworkController, Scheduler, StationManager, IncidentManager, Driver, SystemAdministrator) get demo users and full negative tests. | Permissions are configuration, so extra roles are cheap to model but expensive to test. | Under-tested roles could hide an escalation bug. | Medium | Decided by me | REQ-OPS-001 |
| ASM-014 | MVP crew model has one role: the driver. Guard is deferred. | Scope control. | Low - extension point retained. | Low | Decided by me | — |
| ASM-015 | A Vehicle is a consist (trainset or locomotive plus coaches). Individual coaches are not modelled. | Enough for failure and rotation without a fleet-management system. | Coach-level faults cannot be represented. | Low | Decided by me | REQ-LIVE-010 |
| ASM-031 | Train location is shown as a schematic stop timeline, not a map. | No third-party dependency, works offline, answers the actual question better. | Assessors may expect a map; can be added later. | Low | Decided by me | REQ-PAX-003 |
| ASM-032 | ETA uses offset propagation plus a per-stop dwell model, not machine learning. | Explainable and testable; ML would be unexplainable under live review (brief 28). | Less accurate under unusual conditions. | Low | Decided by me | REQ-LIVE-007 |
| ASM-033 | Station boards sort by expected departure time, with scheduled time retained visibly. | Passengers care when the train will actually leave. | Some operators sort by scheduled time. | Low | Decided by me | REQ-PAX-002 |

## Assumption

| ID | Statement | Why needed | Risk if wrong | Impact | Status | Used by |
|---|---|---|---|---|---|---|
| ASM-010 | 'Station operations' is a role-scoped view of the Ops console, not a separate application. | Avoids a sixth deliverable. | If a separate app was intended the scope grows by roughly 2 days. | Medium | Assumed - pending confirmation | — |
| ASM-011 | Station experience delivers both displays and the interactive terminal from one React codebase in two modes. | Brief 23 allows 'either or both'; shared data layer and tokens make both cheap. | If one is cut, saves about a day but loses the terminal interaction or the offline-display narrative. | Medium | Assumed - pending confirmation | — |
| ASM-012 | Passengers have no accounts in the MVP. Following a journey uses an anonymous device push subscription. | With catering demoted, following a train is the only feature needing identity. | If accounts are required, adds about a day and a privacy surface (reset, deletion). | Medium | Assumed - pending confirmation | REQ-PAX-007 |
| ASM-016 | One railway operator on an isolated network. No freight, no other operators, no cross-border services. | Removes cross-operator data exchange. | None for the demo. | Low | Assumed - pending confirmation | — |
| ASM-017 | Seed network is fictional: about 12 stations, 6 routes, about 40 services, single time zone, no daylight-saving changes. | Enough variety to expose real problems. | A DST region would need extra handling. | Low | Assumed - pending confirmation | REQ-NFR-005 |
| ASM-018 | No undocumented legacy railway systems are assumed; any required interface is modelled explicitly. | Avoids inventing systems. | If a legacy system existed, integration scope increases. | Low | Decided by me | — |
| ASM-019 | Live data is 'stale' after 60 seconds without an accepted update. | Gives 'live' a testable meaning. | Too tight causes constant warnings; too loose hides outages. Tunable in config. | Medium | Assumed - pending confirmation | REQ-LIVE-004, REQ-PAX-005, REQ-DSP-004 |
| ASM-020 | A Running journey becomes Unknown after 300 seconds without an accepted update. | Defines when the system stops claiming to know. | As above. Tunable. | Medium | Assumed - pending confirmation | REQ-LIVE-004, REQ-DSP-004 |
| ASM-021 | Punctuality bands: on time under 2 minutes late, minor delay 2 to 9, major delay 10 or more. Early means 2 or more minutes ahead. | Needed to colour and word status consistently. | Different operator definitions change reporting but not code. | Low | Assumed - pending confirmation | REQ-LIVE-002 |
| ASM-022 | Push is sent when the expected arrival changes by 5 minutes or more, and at most once per 5 minutes per followed journey, always for cancel, platform change and termination. | Prevents notification spam. | Too strict misses useful updates. | Medium | Assumed - pending confirmation | REQ-PAX-007 |
| ASM-023 | Terminal resets to home after 60 seconds idle. | No session left behind. | Too short frustrates slow users. | Low | Assumed - pending confirmation | REQ-TRM-003 |
| ASM-024 | Cancelled trains stay on boards as 'Cancelled' for 15 minutes after scheduled departure; departed trains are removed after 1 minute. | Passengers need to see a cancellation, not a vanished train. | Taste only. | Low | Assumed - pending confirmation | REQ-DSP-007 |
| ASM-025 | Minimum transfer time is 5 minutes at every station. | Needed to judge connections. | Real values vary by station. | Low | Assumed - pending confirmation | REQ-PAX-006 |
| ASM-026 | Journeys are generated nightly for the next 7 days. | Defines how far ahead passengers can look. | Longer horizons need a different data strategy. | Low | Assumed - pending confirmation | REQ-LIVE-008 |
| ASM-027 | Retention: raw telemetry 7 days, aggregated punctuality 13 months, audit 24 months, legal hold overrides for incidents. | Required by brief 20. | Real obligations may differ. | Medium | Assumed - pending confirmation | REQ-DRV-010, REQ-DATA-003 |
| ASM-028 | Public read endpoints are limited to 60 requests per minute per client. | Abuse protection. | May throttle legitimate shared-NAT users. | Low | Assumed - pending confirmation | REQ-SEC-002 |
| ASM-029 | Devices send a heartbeat every 30 seconds and are shown offline after 90 seconds. | Defines 'dead display'. | Network jitter causes false alarms. | Low | Assumed - pending confirmation | REQ-DSP-005 |
| ASM-030 | English plus one secondary language on the passenger app and displays; ops console English only. | Scope control. | Locale needs might differ. | Low | Assumed - pending confirmation | REQ-PAX-013 |
| ASM-034 | No real railway stakeholder can be interviewed. Findings marked 'Domain knowledge' come from general railway-operations knowledge and AI persona simulation, not from the organisation. | Integrity of the evidence: nothing is presented as fact that is not. | Findings may differ from the organisation's actual practice. | Medium | Question sent | OQ-01 |
