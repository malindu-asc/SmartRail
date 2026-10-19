# Spec coverage

> Generated 2026-10-16 by `tools/docs/check_spec_coverage.py` from `specs/coverage-map.csv`. Edit the CSV, not this file.

Every requirement with MVP set to Yes or Thin is owned by exactly one spec: the spec that finishes it. A spec may touch other requirements on the way (a data field in one spec, its behaviour in another); the note says so.

## Summary

- MVP requirements (Yes or Thin): **141**
- Owned by exactly one spec: **141**
- Problems: **0**

| Spec | Title | Status | Requirements |
|---|---|---|---|
| SPEC-001 | Identity foundation (Keycloak, roles, secure by default) | Built; spec to be written retrospectively | 2 |
| SPEC-002 | Network reference data and seed | Verifying | 0 |
| SPEC-003 | Timetable: versions, calendars, services, public timetable | Verifying | 0 |
| SPEC-004 | Journey generation and the journey model (lifecycle, per-stop state, assignments, scheduled board) | In progress | 9 |
| SPEC-005 | Live feed ingest, delay, confidence (Live, Stale, Unknown), source precedence | Verifying | 9 |
| SPEC-006 | Simulator scenarios through the real ingest endpoint | Planned | 5 |
| SPEC-007 | Departure board and journey timeline API, realtime push | Planned | 3 |
| SPEC-008 | Flutter foundation: shared packages, app shell, router, localization | Planned | 3 |
| SPEC-009 | Passenger app: departures board and journey timeline with staleness | Planned | 6 |
| SPEC-010 | Live network board and controller journey actions with reasons and audit: cancel, terminate short, interrupt, resume, reinstate, platform change, driver messages, concurrent-edit detection | Planned | 7 |
| SPEC-011 | Driver app and driver events: duty, stop events, delay reasons, offline queue, replay and reconciliation vs the feed, control message delivery, orphaned events, crew handover | Planned | 11 |
| SPEC-012 | Assignments (vehicle, driver), vehicle unavailability, unassigned alerts, vehicle failure cascade, roles administration, device health and revocation, staff account protection | Planned | 5 |
| SPEC-013 | Platform display (kiosk) | Planned | 8 |
| SPEC-014 | Station search and route stops in the passenger app | Planned | 0 |
| SPEC-015 | Timetable versioning workflow: draft, review, approve, publish (API and console) | Planned | 1 |
| SPEC-016 | Journey search A to B with one transfer (REQ-PAX-006) | Planned | 1 |
| SPEC-017 | Public rate limiting (REQ-SEC-002) and permission-matrix tests (REQ-SEC-003) | Planned | 7 |
| SPEC-018 | Web foundation: shared packages, design tokens, auth, routing, data layer (console and kiosk) | Planned | 0 |
| SPEC-019 | Station terminal: destination search, station information, idle reset, lockdown, offline mode | Planned | 5 |
| SPEC-020 | Notifications: follow a journey, push, inbox, notification event matrix, ticket auto-follow | Proposed | 5 |
| SPEC-021 | Passenger accounts: register, sign in, recover tickets on a new device, delete account and device data, sign-in protection | Proposed | 5 |
| SPEC-022 | Ticketing core: tickets bound to a journey, sales window, fares and categories, ticket states, cancellation affects tickets, signed QR with offline display | Proposed | 9 |
| SPEC-023 | Payments: provider-neutral port with a fake default, idempotent confirmation, authentic callbacks, issue only after payment, unpaid orders expire | Proposed | 5 |
| SPEC-024 | Refunds: full refund after a railway cancellation, refund as its own record, no double refunds, automatic retry | Proposed | 5 |
| SPEC-025 | Ticket inspector mode in the staff app: server-side validation, rejection reasons, past-the-destination, online-only | Proposed | 6 |
| SPEC-026 | Gate scanning: gate device, board and exit scans, retries, excess fare, missing exit, offline, scan log | Proposed | 10 |
| SPEC-027 | Disruptions and incidents: disruption console, second approver, review-by time, scoped resolution, planned works, incident lifecycle, emergency termination, plain-text content, banners and retraction | Proposed | 8 |
| SPEC-028 | Station Master console: own-station view and actions, live access status (lifts, stairs, gates) | Proposed | 2 |
| SPEC-029 | Punctuality report | Proposed | 1 |
| SPEC-030 | Catering menu service (Node): read-only menu with call to order, failure isolated from the railway | Proposed | 3 |

## SPEC-001 Identity foundation (Keycloak, roles, secure by default)

Status: Built; spec to be written retrospectively

| Requirement | Title | Priority | MVP | Note |
|---|---|---|---|---|
| REQ-OPS-001 | Roles and separation of duties | Must | Yes | roles; separation of duties verified per action in 017 |
| REQ-SEC-001 | Four authentication postures | Must | Yes |  |

## SPEC-004 Journey generation and the journey model (lifecycle, per-stop state, assignments, scheduled board)

Status: In progress

| Requirement | Title | Priority | MVP | Note |
|---|---|---|---|---|
| REQ-LIVE-001 | Journey lifecycle states | Must | Yes | table and rules built; Running/Completed driven by feed in 005; interrupt/resume in 010 |
| REQ-LIVE-008 | Journey generation | Must | Yes |  |
| REQ-LIVE-009 | Service date versus calendar date | Must | Yes | board crossing midnight re-checked live in 005 |
| REQ-LIVE-010 | Time-ranged vehicle assignment | Must | Yes | telemetry resolution in 005 |
| REQ-LIVE-017 | Timetable changes apply from effective date | Must | Yes |  |
| REQ-LIVE-019 | Train number is not identity | Should | Yes |  |
| REQ-NFR-005 | Time handling | Must | Yes | time handling |
| REQ-OPS-011 | Append-only audit | Must | Yes | audit building block; used by every action spec |
| REQ-PAX-001 | Public information without sign-in | Must | Yes | public reads without sign-in |

## SPEC-005 Live feed ingest, delay, confidence (Live, Stale, Unknown), source precedence

Status: Verifying

| Requirement | Title | Priority | MVP | Note |
|---|---|---|---|---|
| REQ-LIVE-002 | Punctuality separate from lifecycle | Must | Yes | fields exist from 004; computed in 005 |
| REQ-LIVE-003 | Source and confidence on every estimate | Must | Yes |  |
| REQ-LIVE-004 | Stale and Unknown thresholds | Must | Yes |  |
| REQ-LIVE-005 | Telemetry validation | Must | Yes |  |
| REQ-LIVE-006 | Source precedence on conflict | Must | Yes | driver becomes a third source in 011 |
| REQ-LIVE-007 | ETA recomputation | Must | Yes |  |
| REQ-LIVE-012 | Early running rule | Should | Yes |  |
| REQ-LIVE-015 | Ghost and phantom movements | Should | Yes | internal flag only; ops list in 010 |
| REQ-SEC-004 | Feed source authentication | Must | Yes |  |

## SPEC-006 Simulator scenarios through the real ingest endpoint

Status: Planned

| Requirement | Title | Priority | MVP | Note |
|---|---|---|---|---|
| REQ-SIM-001 | Simulator uses the real ingestion path | Must | Yes |  |
| REQ-SIM-002 | Deterministic, accelerable scenarios | Must | Yes |  |
| REQ-SIM-003 | Fault injection | Must | Yes |  |
| REQ-SIM-004 | Scenario library | Must | Yes |  |
| REQ-SIM-005 | Scenario reset outside production | Should | Yes |  |

## SPEC-007 Departure board and journey timeline API, realtime push

Status: Planned

| Requirement | Title | Priority | MVP | Note |
|---|---|---|---|---|
| REQ-NFR-001 | End-to-end freshness | Must | Yes | end-to-end freshness |
| REQ-NFR-002 | Departures performance | Should | Yes | departures performance |
| REQ-NFR-007 | Realtime fallback | Should | Yes | realtime fallback |

## SPEC-008 Flutter foundation: shared packages, app shell, router, localization

Status: Planned

| Requirement | Title | Priority | MVP | Note |
|---|---|---|---|---|
| REQ-NFR-003 | WCAG 2.2 AA | Must | Yes | WCAG baseline for apps; web in 018 |
| REQ-PAX-013 | Two languages | Should | Yes | two languages in the Flutter foundation |
| REQ-PAX-014 | Screen reader and text scaling | Must | Yes | accessibility baseline; checked in each app spec |

## SPEC-009 Passenger app: departures board and journey timeline with staleness

Status: Planned

| Requirement | Title | Priority | MVP | Note |
|---|---|---|---|---|
| REQ-PAX-002 | Station departure board | Must | Yes |  |
| REQ-PAX-003 | Journey detail timeline | Must | Yes |  |
| REQ-PAX-004 | Status never colour-only | Must | Yes | rule applies to every UI spec |
| REQ-PAX-005 | Staleness labelling | Must | Yes |  |
| REQ-PAX-010 | Offline cache | Should | Yes | offline cache |
| REQ-PAX-011 | Indicative-information disclaimer | Must | Yes |  |

## SPEC-010 Live network board and controller journey actions with reasons and audit: cancel, terminate short, interrupt, resume, reinstate, platform change, driver messages, concurrent-edit detection

Status: Planned

| Requirement | Title | Priority | MVP | Note |
|---|---|---|---|---|
| REQ-LIVE-013 | Interrupted journey shows Unknown ETA | Must | Yes | interrupt and resume; null delay already enforced in 005 |
| REQ-LIVE-014 | Platform per stop with change state | Must | Yes | data per stop built in 004; change action and push in 010 and 007 |
| REQ-LIVE-020 | Platform conflict warning | Could | Thin | platform conflict warning |
| REQ-OPS-002 | Live network board | Must | Yes | live network board |
| REQ-OPS-004 | Journey actions with reasons | Must | Yes | cancel terminate-short interrupt resume reinstate with reasons |
| REQ-OPS-010 | Concurrent edit detection | Should | Yes | concurrent edit detection |
| REQ-OPS-013 | Messages to drivers | Should | Thin | messages to drivers |

## SPEC-011 Driver app and driver events: duty, stop events, delay reasons, offline queue, replay and reconciliation vs the feed, control message delivery, orphaned events, crew handover

Status: Planned

| Requirement | Title | Priority | MVP | Note |
|---|---|---|---|---|
| REQ-DRV-001 | Driver authentication and session | Must | Yes |  |
| REQ-DRV-002 | Duty view, offline-capable | Must | Yes |  |
| REQ-DRV-003 | Permitted driver actions | Must | Yes | departed/arrived/skipped |
| REQ-DRV-004 | No typing between stops | Should | Yes |  |
| REQ-DRV-005 | Offline action queue | Must | Yes |  |
| REQ-DRV-006 | Replay and reconciliation | Must | Yes |  |
| REQ-DRV-007 | Control message delivery states | Must | Yes | driver side; sender is REQ-OPS-013 in 010 |
| REQ-DRV-008 | Orphaned events from an ended duty | Should | Yes |  |
| REQ-DRV-009 | Crew handover mid-journey | Should | Thin |  |
| REQ-DRV-010 | Driver location limits | Must | Yes |  |
| REQ-DRV-011 | Stop events are validated against the journey | Should | Yes |  |

## SPEC-012 Assignments (vehicle, driver), vehicle unavailability, unassigned alerts, vehicle failure cascade, roles administration, device health and revocation, staff account protection

Status: Planned

| Requirement | Title | Priority | MVP | Note |
|---|---|---|---|---|
| REQ-LIVE-011 | Unassigned vehicle or driver alert | Must | Yes | unassigned alerts at 24 h and 2 h |
| REQ-LIVE-018 | Vehicle failure cascades through its rotation | Should | Thin | vehicle failure cascade |
| REQ-OPS-009 | Device health and revocation | Must | Yes | device health and revocation (admin console) |
| REQ-OPS-012 | Vehicle unavailability | Should | Thin | vehicle unavailability |
| REQ-SEC-006 | Staff account protection | Should | Thin | staff account protection |

## SPEC-013 Platform display (kiosk)

Status: Planned

| Requirement | Title | Priority | MVP | Note |
|---|---|---|---|---|
| REQ-DSP-001 | Platform display | Must | Yes |  |
| REQ-DSP-002 | Concourse display | Must | Yes |  |
| REQ-DSP-003 | Device identity and delta stream | Must | Yes |  |
| REQ-DSP-004 | Display offline honesty | Must | Yes |  |
| REQ-DSP-005 | Device heartbeat | Must | Yes |  |
| REQ-DSP-006 | Server-time countdowns | Should | Yes |  |
| REQ-DSP-007 | Cancelled and departed row timing | Should | Yes |  |
| REQ-DSP-008 | Motion and flashing limits | Should | Yes |  |

## SPEC-015 Timetable versioning workflow: draft, review, approve, publish (API and console)

Status: Planned

| Requirement | Title | Priority | MVP | Note |
|---|---|---|---|---|
| REQ-OPS-003 | Timetable versioning and approval | Must | Yes | versioning and approval workflow |

## SPEC-016 Journey search A to B with one transfer (REQ-PAX-006)

Status: Planned

| Requirement | Title | Priority | MVP | Note |
|---|---|---|---|---|
| REQ-PAX-006 | Direct and one-transfer journey search | Should | Yes |  |

## SPEC-017 Public rate limiting (REQ-SEC-002) and permission-matrix tests (REQ-SEC-003)

Status: Planned

| Requirement | Title | Priority | MVP | Note |
|---|---|---|---|---|
| REQ-DATA-001 | Data classification | Must | Yes |  |
| REQ-DATA-002 | Log hygiene | Must | Yes |  |
| REQ-DATA-003 | Retention and legal hold | Must | Yes | retention and legal hold; telemetry purge job |
| REQ-NFR-004 | Observability | Must | Yes | observability |
| REQ-NFR-006 | Degraded-mode matrix | Must | Yes | degraded-mode matrix |
| REQ-SEC-002 | Public rate limiting | Must | Yes |  |
| REQ-SEC-003 | Permission matrix tests | Must | Yes |  |

## SPEC-019 Station terminal: destination search, station information, idle reset, lockdown, offline mode

Status: Planned

| Requirement | Title | Priority | MVP | Note |
|---|---|---|---|---|
| REQ-TRM-001 | Destination search | Must | Yes |  |
| REQ-TRM-002 | Station information and facilities | Should | Yes |  |
| REQ-TRM-003 | Idle reset | Must | Yes |  |
| REQ-TRM-004 | Kiosk lockdown | Should | Yes |  |
| REQ-TRM-005 | Timetable-only offline mode | Should | Yes |  |

## SPEC-020 Notifications: follow a journey, push, inbox, notification event matrix, ticket auto-follow

Status: Proposed

| Requirement | Title | Priority | MVP | Note |
|---|---|---|---|---|
| REQ-NFR-008 | Notification event matrix | Must | Yes | notification event matrix |
| REQ-PAX-007 | Follow a journey and notifications | Must | Yes |  |
| REQ-PAX-008 | Notification content | Must | Yes |  |
| REQ-PAX-015 | Notification inbox and push reliability | Should | Yes |  |
| REQ-TKT-010 | Auto-follow and platform changes | Should | Yes | auto-follow and platform change notices |

## SPEC-021 Passenger accounts: register, sign in, recover tickets on a new device, delete account and device data, sign-in protection

Status: Proposed

| Requirement | Title | Priority | MVP | Note |
|---|---|---|---|---|
| REQ-ACC-001 | Register and sign in | Must | Yes |  |
| REQ-ACC-002 | Recover tickets on a new device | Must | Yes |  |
| REQ-ACC-003 | Delete my account | Should | Thin |  |
| REQ-ACC-004 | Sign-in protection | Should | Thin |  |
| REQ-PAX-012 | Delete device data | Must | Yes | delete device data |

## SPEC-022 Ticketing core: tickets bound to a journey, sales window, fares and categories, ticket states, cancellation affects tickets, signed QR with offline display

Status: Proposed

| Requirement | Title | Priority | MVP | Note |
|---|---|---|---|---|
| REQ-TKT-001 | Ticket bound to a journey, not a time | Must | Yes |  |
| REQ-TKT-002 | Sales window and double availability check | Must | Yes |  |
| REQ-TKT-003 | Fares and passenger categories | Must | Yes |  |
| REQ-TKT-004 | Accessibility flag | Should | Yes |  |
| REQ-TKT-005 | Ticket states and append-only history | Must | Yes |  |
| REQ-TKT-008 | Regional only | Must | Yes |  |
| REQ-TKT-009 | Cancellation affects tickets | Must | Yes | cancellation affects tickets |
| REQ-TKT-011 | Ticket ends with the journey | Should | Yes |  |
| REQ-TKT-012 | Signed QR and offline display | Must | Yes | signed QR and offline display |

## SPEC-023 Payments: provider-neutral port with a fake default, idempotent confirmation, authentic callbacks, issue only after payment, unpaid orders expire

Status: Proposed

| Requirement | Title | Priority | MVP | Note |
|---|---|---|---|---|
| REQ-PAY-001 | Provider-neutral payment with a fake default | Must | Yes |  |
| REQ-PAY-002 | Idempotent payment confirmation | Must | Yes |  |
| REQ-PAY-003 | Authentic payment callbacks | Must | Yes |  |
| REQ-TKT-006 | Issue only after confirmed payment | Must | Yes |  |
| REQ-TKT-007 | Unpaid orders expire | Should | Yes |  |

## SPEC-024 Refunds: full refund after a railway cancellation, refund as its own record, no double refunds, automatic retry

Status: Proposed

| Requirement | Title | Priority | MVP | Note |
|---|---|---|---|---|
| REQ-REF-001 | Request a full refund after a railway cancellation | Must | Yes |  |
| REQ-REF-002 | Refund is its own record | Must | Yes |  |
| REQ-REF-003 | No double refunds | Must | Yes |  |
| REQ-REF-004 | Automatic refund retry and visibility | Should | Thin |  |
| REQ-REF-005 | Automatic refund when no ticket can be issued | Must | Yes |  |

## SPEC-025 Ticket inspector mode in the staff app: server-side validation, rejection reasons, past-the-destination, online-only

Status: Proposed

| Requirement | Title | Priority | MVP | Note |
|---|---|---|---|---|
| REQ-INS-001 | Ticket Inspector role in the staff app | Must | Yes |  |
| REQ-INS-002 | Server-side ticket validation | Must | Yes |  |
| REQ-INS-003 | Unusable tickets are rejected | Must | Yes |  |
| REQ-INS-004 | Travelling past the destination | Must | Yes |  |
| REQ-INS-005 | Inspection needs a connection | Must | Yes |  |
| REQ-INS-006 | Inspector position defaults to the live journey | Should | Yes |  |

## SPEC-026 Gate scanning: gate device, board and exit scans, retries, excess fare, missing exit, offline, scan log

Status: Proposed

| Requirement | Title | Priority | MVP | Note |
|---|---|---|---|---|
| REQ-GAT-001 | Gate scanner device | Must | Yes |  |
| REQ-GAT-002 | Board scan | Must | Yes |  |
| REQ-GAT-003 | Exit scan | Must | Yes |  |
| REQ-GAT-004 | Double scans and retries | Must | Yes |  |
| REQ-GAT-005 | Excess fare shown at the exit | Should | Yes |  |
| REQ-GAT-006 | Missing exit scan | Should | Yes |  |
| REQ-GAT-007 | Scan context | Must | Yes |  |
| REQ-GAT-008 | Gate offline | Must | Yes |  |
| REQ-GAT-009 | Scan log | Must | Yes |  |
| REQ-GAT-010 | Fraud is detected and logged, handled in person | Must | Yes |  |

## SPEC-027 Disruptions and incidents: disruption console, second approver, review-by time, scoped resolution, planned works, incident lifecycle, emergency termination, plain-text content, banners and retraction

Status: Proposed

| Requirement | Title | Priority | MVP | Note |
|---|---|---|---|---|
| REQ-LIVE-016 | Scoped disruption resolution | Should | Yes | section/route/station disruption resolves to journeys |
| REQ-OPS-005 | Disruption console | Must | Yes |  |
| REQ-OPS-006 | Disruption review-by time | Should | Yes |  |
| REQ-OPS-007 | Incident lifecycle | Must | Yes | incident lifecycle |
| REQ-OPS-008 | Major incident (emergency termination) | Must | Thin | emergency termination |
| REQ-OPS-016 | Planned disruptions (engineering works) | Should | Thin | planned disruptions |
| REQ-PAX-009 | Disruption banners and retraction | Must | Yes | banners and retraction; shown in 009 |
| REQ-SEC-005 | Plain-text disruption content | Must | Yes | plain-text disruption content |

## SPEC-028 Station Master console: own-station view and actions, live access status (lifts, stairs, gates)

Status: Proposed

| Requirement | Title | Priority | MVP | Note |
|---|---|---|---|---|
| REQ-OPS-018 | Station Master: own-station view and actions | Must | Yes | station master own-station view |
| REQ-OPS-019 | Live access status (lifts, stairs, gates) | Should | Yes | lifts stairs gates status |

## SPEC-029 Punctuality report

Status: Proposed

| Requirement | Title | Priority | MVP | Note |
|---|---|---|---|---|
| REQ-OPS-014 | Punctuality report | Should | Yes | punctuality report |

## SPEC-030 Catering menu service (Node): read-only menu with call to order, failure isolated from the railway

Status: Proposed

| Requirement | Title | Priority | MVP | Note |
|---|---|---|---|---|
| REQ-INT-001 | Separate catering menu service | Could | Thin |  |
| REQ-INT-004 | Menu failure never affects the railway | Could | Thin |  |
| REQ-INT-005 | Food menu and call to order | Could | Thin |  |
