# Traceability

> **Generated 2026-10-05** by `tools/docs/build_traceability.py` — do not edit by hand. Re-run after changing any requirement, story, use case, flow, edge case or test.

Tests are linked by mentioning the requirement id in the test (name, attribute or comment), e.g. `[Trait("Req", "REQ-LIVE-004")]` in xUnit or `test('REQ-PAX-002 board shows delta', …)` in Playwright / Flutter.

## Coverage

| Measure | Count |
|---|---|
| Requirements | 102 |
| Must | 66 |
| Must with at least one story | 66 |
| Must with a use case, flow or specification document | 66 |
| Must with at least one test | 0 |
| All requirements with at least one test | 0 |

## Must requirements without a story

None ✅

## Must requirements without a use case, flow or specification

None ✅

## Must requirements without a test

REQ-LIVE-001, REQ-LIVE-002, REQ-LIVE-003, REQ-LIVE-004, REQ-LIVE-005, REQ-LIVE-006, REQ-LIVE-007, REQ-LIVE-008, REQ-LIVE-009, REQ-LIVE-010, REQ-LIVE-011, REQ-LIVE-013, REQ-LIVE-014, REQ-LIVE-017, REQ-PAX-001, REQ-PAX-002, REQ-PAX-003, REQ-PAX-004, REQ-PAX-005, REQ-PAX-007, REQ-PAX-008, REQ-PAX-009, REQ-PAX-011, REQ-PAX-012, REQ-PAX-014, REQ-DRV-001, REQ-DRV-002, REQ-DRV-003, REQ-DRV-005, REQ-DRV-006, REQ-DRV-007, REQ-DRV-010, REQ-DSP-001, REQ-DSP-002, REQ-DSP-003, REQ-DSP-004, REQ-DSP-005, REQ-TRM-001, REQ-TRM-003, REQ-OPS-001, REQ-OPS-002, REQ-OPS-003, REQ-OPS-004, REQ-OPS-005, REQ-OPS-007, REQ-OPS-008, REQ-OPS-009, REQ-OPS-011, REQ-SEC-001, REQ-SEC-002, REQ-SEC-003, REQ-SEC-004, REQ-SEC-005, REQ-DATA-001, REQ-DATA-002, REQ-DATA-003, REQ-NFR-001, REQ-NFR-003, REQ-NFR-004, REQ-NFR-005, REQ-NFR-006, REQ-NFR-008, REQ-SIM-001, REQ-SIM-002, REQ-SIM-003, REQ-SIM-004 *(expected before the build starts)*

## Matrix

| Requirement | Title | Pri | MVP | Status | Stories | Use cases | Flows | Specs | Edge cases | Tests |
|---|---|---|---|---|---|---|---|---|---|---|
| REQ-LIVE-001 | Journey lifecycle states | Must | Yes | Proposed | US-083 | UC-PAX-02 | CF-02 | 00-requirements/04-domain/state-models.md | EDG-057 | — |
| REQ-LIVE-002 | Punctuality separate from lifecycle | Must | Yes | Proposed | US-083 | UC-PAX-02 | — | — | EDG-027, EDG-035 | — |
| REQ-LIVE-003 | Source and confidence on every estimate | Must | Yes | Proposed | US-003 | UC-PAX-02 | — | — | — | — |
| REQ-LIVE-004 | Stale and Unknown thresholds | Must | Yes | Proposed | US-002, US-030 | UC-OPS-01, UC-PAX-02 | CF-06 | 00-requirements/04-domain/system-context.md, 01-ux/README.md, 03-qa/README.md | EDG-031, EDG-032 | — |
| REQ-LIVE-005 | Telemetry validation | Must | Yes | Proposed | US-081 | — | CF-01 | 00-requirements/04-domain/source-precedence.md, 00-requirements/04-domain/system-context.md | EDG-051, EDG-052, EDG-053 | — |
| REQ-LIVE-006 | Source precedence on conflict | Must | Yes | Proposed | US-023 | UC-DRV-02 | CF-01, CF-03 | 00-requirements/04-domain/source-precedence.md | EDG-040, EDG-042, EDG-046, EDG-058 | — |
| REQ-LIVE-007 | ETA recomputation | Must | Yes | Proposed | US-003 | — | CF-01 | — | — | — |
| REQ-LIVE-008 | Journey generation | Must | Yes | Proposed | US-084 | UC-OPS-05 | — | 00-requirements/04-domain/state-models.md | EDG-003, EDG-004, EDG-006 | — |
| REQ-LIVE-009 | Service date versus calendar date | Must | Yes | Proposed | US-084 | UC-PAX-02 | — | 00-requirements/04-domain/glossary.md | EDG-002 | — |
| REQ-LIVE-010 | Time-ranged vehicle assignment | Must | Yes | Proposed | US-082 | UC-OPS-06, UC-PAX-02 | CF-07 | 00-requirements/04-domain/identity-model.md | EDG-014, EDG-033 | — |
| REQ-LIVE-011 | Unassigned vehicle or driver alert | Must | Yes | Proposed | US-051 | UC-OPS-06 | CF-07 | 00-requirements/04-domain/identity-model.md | EDG-010, EDG-011, EDG-017 | — |
| REQ-LIVE-012 | Early running rule | Should | Yes | Proposed | — | UC-PAX-01 | — | 00-requirements/04-domain/state-models.md | EDG-024 | — |
| REQ-LIVE-013 | Interrupted journey shows Unknown ETA | Must | Yes | Proposed | US-004 | UC-PAX-01, UC-PAX-02 | CF-07 | 00-requirements/04-domain/identity-model.md, 01-ux/README.md | EDG-030 | — |
| REQ-LIVE-014 | Platform per stop with change state | Must | Yes | Proposed | US-032 | UC-OPS-03 | CF-05 | 00-requirements/04-domain/state-models.md | EDG-020, EDG-021 | — |
| REQ-LIVE-015 | Ghost and phantom movements | Should | Yes | Proposed | — | UC-OPS-01 | — | 00-requirements/04-domain/identity-model.md | EDG-054, EDG-055 | — |
| REQ-LIVE-016 | Scoped disruption resolution | Should | Yes | Proposed | US-040 | UC-DSP-02, UC-OPS-04 | CF-04, CF-08 | — | EDG-034, EDG-038, EDG-039, EDG-067, EDG-088 | — |
| REQ-LIVE-017 | Timetable changes apply from effective date | Must | Yes | Proposed | US-050 | UC-OPS-05 | — | 00-requirements/04-domain/state-models.md | EDG-001 | — |
| REQ-LIVE-018 | Vehicle failure cascades through its rotation | Should | Thin | Proposed | US-051 | UC-OPS-07 | CF-07 | 00-requirements/04-domain/identity-model.md, 00-requirements/04-domain/system-context.md | EDG-016 | — |
| REQ-LIVE-019 | Train number is not identity | Should | Yes | Proposed | — | UC-PAX-02 | — | 00-requirements/04-domain/glossary.md, 00-requirements/04-domain/identity-model.md | EDG-009, EDG-065 | — |
| REQ-LIVE-020 | Platform conflict warning | Could | Thin | Proposed | US-032 | UC-OPS-03 | CF-05 | — | EDG-022 | — |
| REQ-PAX-001 | Public information without sign-in | Must | Yes | Proposed | US-001 | UC-PAX-01 | — | 00-requirements/04-domain/system-context.md | — | — |
| REQ-PAX-002 | Station departure board | Must | Yes | Proposed | US-001 | UC-PAX-01 | CF-01 | 01-ux/README.md, 03-qa/README.md | — | — |
| REQ-PAX-003 | Journey detail timeline | Must | Yes | Proposed | US-003 | UC-PAX-02 | — | — | EDG-066 | — |
| REQ-PAX-004 | Status never colour-only | Must | Yes | Proposed | US-001 | UC-DSP-02, UC-PAX-01 | — | 01-ux/README.md | — | — |
| REQ-PAX-005 | Staleness labelling | Must | Yes | Proposed | US-002 | UC-PAX-01, UC-PAX-06 | CF-06 | 00-requirements/04-domain/system-context.md | — | — |
| REQ-PAX-006 | Direct and one-transfer journey search | Should | Yes | Proposed | US-005 | UC-PAX-03, UC-TRM-01 | — | — | EDG-007, EDG-062, EDG-063 | — |
| REQ-PAX-007 | Follow a journey and notifications | Must | Yes | Proposed | US-010, US-011 | UC-PAX-04 | CF-01, CF-02, CF-05 | — | EDG-070, EDG-073, EDG-075 | — |
| REQ-PAX-008 | Notification content | Must | Yes | Proposed | US-011 | UC-PAX-04 | — | 00-requirements/06-requirements/notification-matrix.md | — | — |
| REQ-PAX-009 | Disruption banners and retraction | Must | Yes | Proposed | US-012, US-041 | UC-OPS-04, UC-PAX-04, UC-PAX-05, UC-TRM-03 | CF-04 | 00-requirements/04-domain/system-context.md | EDG-072 | — |
| REQ-PAX-010 | Offline cache | Should | Yes | Proposed | US-006 | UC-PAX-01, UC-PAX-06 | — | — | EDG-060 | — |
| REQ-PAX-011 | Indicative-information disclaimer | Must | Yes | Proposed | US-003 | UC-PAX-02 | — | 00-requirements/06-requirements/data-classification.md | — | — |
| REQ-PAX-012 | Delete device data | Must | Yes | Proposed | US-014 | UC-PAX-07 | — | 00-requirements/06-requirements/data-classification.md | — | — |
| REQ-PAX-013 | Two languages | Should | Yes | Proposed | — | — | — | — | — | — |
| REQ-PAX-014 | Screen reader and text scaling | Must | Yes | Proposed | US-007 | — | — | 01-ux/README.md | EDG-064 | — |
| REQ-PAX-015 | Notification inbox and push reliability | Should | Yes | Proposed | US-013 | UC-PAX-04 | CF-01 | 00-requirements/06-requirements/degraded-mode-matrix.md, 00-requirements/06-requirements/notification-matrix.md | EDG-071 | — |
| REQ-DRV-001 | Driver authentication and session | Must | Yes | Proposed | US-020 | UC-DRV-01 | — | 00-requirements/06-requirements/permission-matrix.md | EDG-015 | — |
| REQ-DRV-002 | Duty view, offline-capable | Must | Yes | Proposed | US-020 | UC-DRV-01 | — | — | EDG-012 | — |
| REQ-DRV-003 | Permitted driver actions | Must | Yes | Proposed | US-021, US-022, US-025 | UC-DRV-02, UC-DRV-03, UC-DRV-06 | — | — | EDG-043, EDG-050 | — |
| REQ-DRV-004 | No typing between stops | Should | Yes | Proposed | US-021 | UC-DRV-02 | — | 00-requirements/04-domain/state-models.md, 01-ux/README.md | — | — |
| REQ-DRV-005 | Offline action queue | Must | Yes | Proposed | US-023 | UC-DRV-04 | CF-03 | — | EDG-044, EDG-045 | — |
| REQ-DRV-006 | Replay and reconciliation | Must | Yes | Proposed | US-023 | UC-DRV-04 | CF-03 | — | EDG-041, EDG-046, EDG-048 | — |
| REQ-DRV-007 | Control message delivery states | Must | Yes | Proposed | US-024 | UC-DRV-04, UC-DRV-05, UC-OPS-09 | CF-03 | 00-requirements/04-domain/state-models.md | EDG-049 | — |
| REQ-DRV-008 | Orphaned events from an ended duty | Should | Yes | Proposed | — | UC-DRV-01, UC-DRV-04 | CF-03 | 00-requirements/04-domain/identity-model.md, 00-requirements/04-domain/state-models.md | EDG-013 | — |
| REQ-DRV-009 | Crew handover mid-journey | Should | Thin | Proposed | — | UC-DRV-04, UC-DRV-07 | CF-03 | 00-requirements/04-domain/identity-model.md | EDG-047 | — |
| REQ-DRV-010 | Driver location limits | Must | Yes | Proposed | US-026 | — | — | 00-requirements/06-requirements/data-classification.md | — | — |
| REQ-DSP-001 | Platform display | Must | Yes | Proposed | US-060 | UC-DSP-01 | CF-05 | 01-ux/README.md | — | — |
| REQ-DSP-002 | Concourse display | Must | Yes | Proposed | US-065 | UC-DSP-02 | — | — | EDG-087 | — |
| REQ-DSP-003 | Device identity and delta stream | Must | Yes | Proposed | US-071 | UC-DSP-03, UC-DSP-04, UC-OPS-10 | — | 04-security/README.md | EDG-084 | — |
| REQ-DSP-004 | Display offline honesty | Must | Yes | Proposed | US-061 | UC-DSP-03 | CF-06 | 00-requirements/04-domain/system-context.md | EDG-080 | — |
| REQ-DSP-005 | Device heartbeat | Must | Yes | Proposed | US-062 | UC-DSP-04, UC-OPS-10 | — | 06-operations/README.md | EDG-082 | — |
| REQ-DSP-006 | Server-time countdowns | Should | Yes | Proposed | — | UC-DSP-01 | — | — | EDG-081 | — |
| REQ-DSP-007 | Cancelled and departed row timing | Should | Yes | Proposed | — | UC-DSP-01, UC-PAX-01 | CF-02 | — | — | — |
| REQ-DSP-008 | Motion and flashing limits | Should | Yes | Proposed | US-060 | UC-DSP-01 | — | 01-ux/README.md | — | — |
| REQ-TRM-001 | Destination search | Must | Yes | Proposed | US-063 | UC-TRM-01 | — | — | — | — |
| REQ-TRM-002 | Station information and facilities | Should | Yes | Proposed | US-063 | UC-TRM-02 | — | — | — | — |
| REQ-TRM-003 | Idle reset | Must | Yes | Proposed | US-064 | UC-TRM-04 | — | — | EDG-085 | — |
| REQ-TRM-004 | Kiosk lockdown | Should | Yes | Proposed | US-064 | UC-TRM-04 | — | — | EDG-086 | — |
| REQ-TRM-005 | Timetable-only offline mode | Should | Yes | Proposed | — | UC-TRM-05 | CF-06 | — | EDG-080 | — |
| REQ-OPS-001 | Roles and separation of duties | Must | Yes | Proposed | US-070 | UC-OPS-13 | — | 00-requirements/04-domain/system-context.md, 00-requirements/06-requirements/permission-matrix.md, 04-security/README.md | EDG-096 | — |
| REQ-OPS-002 | Live network board | Must | Yes | Proposed | US-030 | UC-OPS-01 | — | — | — | — |
| REQ-OPS-003 | Timetable versioning and approval | Must | Yes | Proposed | US-050 | UC-OPS-05 | — | 00-requirements/04-domain/state-models.md, 00-requirements/06-requirements/permission-matrix.md | EDG-005 | — |
| REQ-OPS-004 | Journey actions with reasons | Must | Yes | Proposed | US-031 | UC-OPS-02 | CF-02 | — | EDG-023, EDG-025, EDG-026, EDG-036, EDG-095 | — |
| REQ-OPS-005 | Disruption console | Must | Yes | Proposed | US-040, US-041 | UC-OPS-04, UC-PAX-05 | CF-04 | 00-requirements/04-domain/state-models.md | EDG-091, EDG-092 | — |
| REQ-OPS-006 | Disruption review-by time | Should | Yes | Proposed | US-042 | UC-OPS-04, UC-PAX-05 | CF-04 | 00-requirements/04-domain/state-models.md | EDG-093 | — |
| REQ-OPS-007 | Incident lifecycle | Must | Yes | Proposed | US-043 | UC-OPS-08 | CF-08 | 00-requirements/04-domain/state-models.md | EDG-094 | — |
| REQ-OPS-008 | Major incident (emergency termination) | Must | Thin | Proposed | US-043 | UC-OPS-08, UC-PAX-02 | CF-08 | 00-requirements/06-requirements/data-classification.md | EDG-037 | — |
| REQ-OPS-009 | Device health and revocation | Must | Yes | Proposed | US-062, US-071 | UC-DSP-04, UC-OPS-10 | — | — | — | — |
| REQ-OPS-010 | Concurrent edit detection | Should | Yes | Proposed | — | UC-OPS-02 | CF-02 | 00-requirements/04-domain/source-precedence.md | EDG-090 | — |
| REQ-OPS-011 | Append-only audit | Must | Yes | Proposed | US-031 | UC-OPS-02, UC-OPS-13, UC-OPS-14 | CF-02 | 04-security/README.md | — | — |
| REQ-OPS-012 | Vehicle unavailability | Should | Thin | Proposed | — | UC-OPS-07 | CF-07 | 00-requirements/04-domain/identity-model.md | EDG-010 | — |
| REQ-OPS-013 | Messages to drivers | Should | Thin | Proposed | US-033 | UC-DRV-05, UC-OPS-09 | — | — | — | — |
| REQ-OPS-014 | Punctuality report | Should | Yes | Proposed | — | UC-OPS-11, UC-OPS-14 | — | — | EDG-098 | — |
| REQ-OPS-015 | Customer service lookup | Could | Thin | Proposed | — | UC-OPS-12 | — | — | — | — |
| REQ-OPS-016 | Planned disruptions (engineering works) | Should | Thin | Proposed | US-052 | UC-OPS-15, UC-PAX-03, UC-TRM-03 | — | 00-requirements/04-domain/state-models.md | EDG-074 | — |
| REQ-OPS-017 | Time-boxed elevated access | Could | No | Proposed | — | — | — | — | — | — |
| REQ-SEC-001 | Four authentication postures | Must | Yes | Proposed | US-070 | — | — | 02-architecture/README.md, 04-security/README.md | — | — |
| REQ-SEC-002 | Public rate limiting | Must | Yes | Proposed | US-073 | — | — | 00-requirements/04-domain/system-context.md, 04-security/README.md | — | — |
| REQ-SEC-003 | Permission matrix tests | Must | Yes | Proposed | US-070 | — | — | 00-requirements/06-requirements/permission-matrix.md, 04-security/README.md | EDG-097 | — |
| REQ-SEC-004 | Feed source authentication | Must | Yes | Proposed | US-072 | — | — | 00-requirements/04-domain/source-precedence.md, 00-requirements/04-domain/system-context.md, 02-architecture/README.md +2 | EDG-056 | — |
| REQ-SEC-005 | Plain-text disruption content | Must | Yes | Proposed | US-074 | UC-OPS-04 | CF-04 | 04-security/README.md | EDG-105 | — |
| REQ-SEC-006 | Staff account protection | Should | Thin | Proposed | — | UC-OPS-13 | — | 04-security/README.md | — | — |
| REQ-DATA-001 | Data classification | Must | Yes | Proposed | US-075 | — | — | 00-requirements/06-requirements/data-classification.md | — | — |
| REQ-DATA-002 | Log hygiene | Must | Yes | Proposed | US-075 | — | — | 00-requirements/06-requirements/data-classification.md, 06-operations/README.md | — | — |
| REQ-DATA-003 | Retention and legal hold | Must | Yes | Proposed | US-043 | UC-OPS-08 | CF-08 | 00-requirements/06-requirements/data-classification.md, 06-operations/README.md | — | — |
| REQ-NFR-001 | End-to-end freshness | Must | Yes | Proposed | US-030 | — | CF-01 | 06-operations/README.md | — | — |
| REQ-NFR-002 | Departures performance | Should | Yes | Proposed | — | — | — | — | EDG-103 | — |
| REQ-NFR-003 | WCAG 2.2 AA | Must | Yes | Proposed | US-007 | — | — | 01-ux/README.md | — | — |
| REQ-NFR-004 | Observability | Must | Yes | Proposed | US-085 | — | — | 03-qa/README.md, 06-operations/README.md | — | — |
| REQ-NFR-005 | Time handling | Must | Yes | Proposed | US-080 | — | — | 02-architecture/README.md, 02-architecture/simulator-design.md, 03-qa/README.md | — | — |
| REQ-NFR-006 | Degraded-mode matrix | Must | Yes | Proposed | US-086 | — | CF-06 | 00-requirements/06-requirements/degraded-mode-matrix.md, 01-ux/README.md | EDG-099, EDG-100, EDG-101 | — |
| REQ-NFR-007 | Realtime fallback | Should | Yes | Proposed | — | UC-DSP-03 | — | 00-requirements/06-requirements/degraded-mode-matrix.md, 02-architecture/README.md | EDG-083, EDG-102 | — |
| REQ-NFR-008 | Notification event matrix | Must | Yes | Proposed | US-011 | — | — | 00-requirements/06-requirements/notification-matrix.md | — | — |
| REQ-SIM-001 | Simulator uses the real ingestion path | Must | Yes | Proposed | US-080 | — | — | 00-requirements/04-domain/system-context.md, 02-architecture/README.md, 02-architecture/simulator-design.md +1 | — | — |
| REQ-SIM-002 | Deterministic, accelerable scenarios | Must | Yes | Proposed | US-080 | — | — | 02-architecture/simulator-design.md, 03-qa/README.md | — | — |
| REQ-SIM-003 | Fault injection | Must | Yes | Proposed | US-081 | — | — | 02-architecture/simulator-design.md, 03-qa/README.md | — | — |
| REQ-SIM-004 | Scenario library | Must | Yes | Proposed | US-080 | — | — | 02-architecture/simulator-design.md | — | — |
| REQ-SIM-005 | Scenario reset outside production | Should | Yes | Proposed | — | — | — | 02-architecture/simulator-design.md, 03-qa/README.md | — | — |
| REQ-INT-001 | Separate catering system | Could | Thin | Proposed | — | — | — | — | — | — |
| REQ-INT-002 | Outbound reference and ETA feed | Could | Thin | Proposed | US-091 | UC-EXT-01 | CF-01, CF-02 | 00-requirements/06-requirements/data-classification.md | EDG-113, EDG-114 | — |
| REQ-INT-003 | Signed inbound webhooks | Could | Thin | Proposed | US-091 | UC-EXT-03 | — | 04-security/README.md | EDG-111, EDG-112, EDG-116 | — |
| REQ-INT-004 | Outbox, retry and dead-letter | Could | Thin | Proposed | US-090 | UC-EXT-05 | CF-01 | 00-requirements/04-domain/system-context.md, 00-requirements/06-requirements/degraded-mode-matrix.md, 02-architecture/README.md | EDG-110 | — |
| REQ-INT-005 | Single-screen collection order | Could | Thin | Proposed | US-092 | UC-EXT-02 | — | — | EDG-115 | — |
