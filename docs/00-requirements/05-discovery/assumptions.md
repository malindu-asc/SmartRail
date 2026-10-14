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
| ASM-005 | Catering is a **minimal read-only integration**: a separate service with a seeded menu (categories, items, prices) and a phone number per catering station; the passenger app shows the menu and offers call-to-order. Ordering, payment, collection and ETA/webhook exchange are not built. | User decision 2026-10-07: not a priority, but the brief lists an external integration; a menu with call-to-order keeps the boundary demonstrable at minimum cost. | Weaker than a full integration for the section 26 Integration row; the boundary, the API key and failure isolation are still shown. | Medium | Decided by user | REQ-INT-001, REQ-INT-004, REQ-INT-005 |
| ASM-006 | There is no single all-powerful administrator; roles are divided by accountability. | User instruction, consistent with brief 9. | None. | Low | Decided by user | REQ-OPS-001 |
| ASM-007 | Backend is .NET clean architecture, web is React, mobile is Flutter feature-based clean architecture. | User instruction. | None. | Low | Decided by user | — |

## Decision (me)

| ID | Statement | Why needed | Risk if wrong | Impact | Status | Used by |
|---|---|---|---|---|---|---|
| ASM-008 | The simulator represents the infrastructure / train-detection feed (signalling), not a GPS device. The driver app is supplementary. | Real railways derive position from trackside detection. It also means no single client's connectivity can blind passengers. | If the assessor expects device GPS, the narrative changes but the architecture still works (add a second source). | High | Assumed - pending confirmation | ADR-0006 |
| ASM-009 | SmartRail is an information system. It is not a safety-critical system and never replaces signalling, radio or safety procedures. | Prevents the design implying safety guarantees it cannot give, especially for crash handling. | If treated as safety-critical the entire assurance and certification burden changes. | High | Decided by me | — |
| ASM-013 | Seven roles have accounts in the MVP — Passenger, Driver, TicketInspector, NetworkController, StationMaster, Scheduler, SystemAdministrator — each with demo users and full negative tests. IncidentManager and ReportingAnalyst are capability bundles held by NetworkController, FleetController is held by Scheduler, and CustomerService is deferred (ASM-045). | Roles are permission bundles; the Station Master was restored because gates and station work are real, scoped duties. | The Network Controller still holds a wide set of capabilities. | Medium | Decided by user | REQ-OPS-001 |
| ASM-014 | MVP crew model has one role: the driver. Guard is deferred. | Scope control. | Low - extension point retained. | Low | Decided by me | — |
| ASM-015 | A Vehicle is a consist (trainset or locomotive plus coaches). Individual coaches are not modelled. | Enough for failure and rotation without a fleet-management system. | Coach-level faults cannot be represented. | Low | Decided by me | REQ-LIVE-010 |
| ASM-031 | Train location is shown as a schematic stop timeline, not a map. | No third-party dependency, works offline, answers the actual question better. | Assessors may expect a map; can be added later. | Low | Decided by me | REQ-PAX-003 |
| ASM-032 | ETA uses offset propagation plus a per-stop dwell model, not machine learning. | Explainable and testable; ML would be unexplainable under live review (brief 28). | Less accurate under unusual conditions. | Low | Decided by me | REQ-LIVE-007 |
| ASM-033 | Station boards sort by expected departure time, with scheduled time retained visibly. | Passengers care when the train will actually leave. | Some operators sort by scheduled time. | Low | Decided by me | REQ-PAX-002 |

## Assumption

| ID | Statement | Why needed | Risk if wrong | Impact | Status | Used by |
|---|---|---|---|---|---|---|
| ASM-010 | 'Station operations' is the Station Master's own-station view of the same ops console, not a separate application. | Avoids a sixth deliverable while giving the station a scoped owner. | If a separate app was intended the scope grows by roughly 2 days. | Medium | Decided by user | REQ-OPS-018 |
| ASM-011 | The station experience is one React codebase in three modes: display, terminal and **gate scanner**. | Brief 23 allows 'either or both'; a shared data layer and tokens make all three cheap. | Dropping a mode saves about a day. | Medium | Decided by user | REQ-DSP-003, REQ-TRM-001, REQ-GAT-001 |
| ASM-012 | Passenger accounts exist for ticketing. Boards, search, journey detail, following and disruptions remain anonymous. (Supersedes 'no passenger accounts'.) | Tickets are the main demo flow and need an owner (brief section 14). | Adds sign-in, lockout, deletion and a Personal-data surface. | Medium | Decided by user | REQ-ACC-001 … 004 |
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

## Ticketing and catering decisions (added 2026-10-06)

Decision log: [12-ticketing/README.md](../12-ticketing/README.md).

| ID | Statement | Why needed | Risk if wrong | Impact | Status | Used by |
|---|---|---|---|---|---|---|
| ASM-035 | Ticketing, accounts and payments are in the MVP as a thin, gated vertical slice. | Tickets are the main flow the demo must show. | Displaces graded work (E2E, security, accessibility, docs); mitigated by the D14 gate and cut order. | High | Decided by user | REQ-ACC, REQ-TKT, REQ-PAY, REQ-REF, REQ-INS |
| ASM-036 | Only Regional (unreserved) journeys are purchasable. Intercity and High-speed are shown but not sold. | Avoids seat inventory, holds and concurrency. | The demo cannot show a reserved seat. | Medium | Decided by me | REQ-TKT-008 |
| ASM-037 | The fake payment provider is the default; the PayHere sandbox adapter is a stretch. All payments use a hosted checkout and a signed callback. | No dependency on external keys; no card data in SmartRail. | Assessors may expect a real provider sandbox. | Medium | Decided by me | REQ-PAY-001 … 004 |
| ASM-038 | Refunds after a railway cancellation are auto-approved; staff only view and retry failures. | Eligibility is a fact; removes an approval workflow. | Some organisations require human sign-off. | Low | Decided by me | REQ-REF-001 … 005 |
| ASM-039 | Fares are a seeded stage-based table in LKR; multipliers are Adult 1.0, Child 0.5, Senior 0.5. The values are placeholders. | A deterministic, explainable price. | Real fare rules differ; only seed data changes. | Low | Assumed - pending confirmation | REQ-TKT-003 |
| ASM-040 | An unpaid order expires after 15 minutes. | Abandoned checkouts must not linger. | Too short frustrates slow payers. | Low | Assumed - pending confirmation | REQ-TKT-007 |
| ASM-041 | Ticket validation needs connectivity; offline the inspector sees UNABLE_TO_VERIFY. The signed QR proves authenticity but not current status. | Revocation cannot be checked offline. | Poor-coverage routes make scanning unreliable. | Medium | Decided by me | REQ-TKT-012, REQ-INS-002, REQ-INS-005 |
| ASM-042 | Sales close when the journey departs the passenger's origin stop, or the journey is cancelled. A delay never closes sales. | A delayed train stays buyable. | A passenger may buy after the train physically left a late stop. | Low | Decided by me | REQ-TKT-002 |
| ASM-043 | One role is added: TicketInspector (`ticket.inspect`, a mode of the staff app). No refund role and no customer-service role exist; refund recovery is automatic (ASM-046). Drivers are not inspectors by default. | Inspection needs an owner; refund recovery does not need a person. | A third person-role to test. | Low | Decided by user | REQ-INS-001, REQ-REF-004 |
| ASM-044 | Catering is minimal: a read-only menu plus call-to-order (ASM-005). The fuller boundary design (signed webhooks, outbox, ETA feed) is retained in the docs only. | User priority decision. | See ASM-005. | Medium | Decided by user | REQ-INT-002, REQ-INT-003 (Rejected) |
| ASM-045 | **Role consolidation (revised 2026-10-07).** The MVP builds seven roles (Passenger, Driver, Ticket Inspector, Network Controller, Station Master, Scheduler, System Administrator). Incident Manager and Reporting are capabilities of the Network Controller; Fleet is a capability of the Scheduler; Customer Service is deferred. Full justification in [mvp-roles.md](../06-requirements/mvp-roles.md). | Keeps the build to what the demo needs while authority stays divided by accountability. | The Network Controller concentrates power for incidents; mitigated by audit, a second approver for network-wide alerts and a confirmation step for emergencies. | High | Decided by user | REQ-OPS-001 |
| ASM-046 | A refund that fails at the provider is retried automatically (3 attempts with backoff). The passenger can request it again (idempotent). The Network Controller sees refunds read-only. No staff action is needed. | No customer-service or refund role is built. | After the retries a refund stays FAILED until the provider recovers or the passenger asks again. | Low | Decided by me | REQ-REF-004 |
| ASM-047 | Declaring a Critical incident or an emergency termination is held by the Network Controller, behind a confirmation step, a reason and an audit entry. A production system would split this into a dedicated Incident Manager. | The dedicated role is not built. | One person can declare an emergency alone; mitigated by confirmation and audit. | Medium | Decided by me | REQ-OPS-007, REQ-OPS-008 |
| ASM-048 | **Gates are screens, not barriers.** A gate scanner is the kiosk app in gate mode, scanning a QR by camera or test input and showing the server's result. No physical gate hardware. | Demonstrates board and exit validation without hardware. | Assessors may expect a physical gate; the endpoint behaves identically. | Medium | Decided by user | REQ-GAT-001 |
| ASM-049 | The excess fare at an exit past the destination is (fare to the exit station − fare paid) + a **flat penalty of LKR 500** (placeholder). It is shown and logged only; collection is in person. | Fares are known station to station. | The real penalty rule will differ; only seed data changes. | Low | Assumed - pending confirmation | REQ-GAT-005 |
| ASM-050 | **Fraud is detected and logged by the system, and handled physically** by staff. No fines, blocking or payment collection in the system. | Responding to fraud is a railway procedure. | None for the demo. | Low | Decided by user | REQ-GAT-010 |
| ASM-051 | Ticket inspection is by **spot check**, on the inspector's own phone, using the staff app's inspector mode; the gates cover everyone. | One inspector cannot scan every passenger. | If inspectors are the only check, coverage is partial. | Low | Decided by user | REQ-INS-001 |
| ASM-052 | **Customer information is handled by the call center** outside the system; the call center reads the same public information as passengers. | No customer-service role is built. | A call-center tool would be needed in production. | Low | Decided by user | mvp-roles.md |
| ASM-053 | Push notifications use Firebase on Android if time allows; otherwise the in-app inbox and live banner are the demonstrated channel. | Real push needs a Firebase project. | The demo shows alerts in-app only. | Low | Decided by me | REQ-PAX-015 |
| ASM-054 | **Call to order:** the provider's phone number for each catering station is seeded in the menu service and the app offers tap-to-call. Ordering and payment happen outside SmartRail, by phone, with the provider. | The provider owns the order and the payment. | Passengers must phone; no in-app ordering. | Low | Decided by user | REQ-INT-005 |
