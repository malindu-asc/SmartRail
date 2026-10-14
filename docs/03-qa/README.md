# Quality assurance

**Brief:** §18, §23 QA (strategy covering the full spectrum, functional scenarios, **5 automated end-to-end flows**,
defects, evidence, quality assessment). **Status:** Planned — strategy on D5, automation on D16.

## Approach

Risk-based, not coverage-based. Heavy where the rules are (domain), selective where the contracts are
(integration), deliberate at the top (E2E).

| Level | Tools | Focus |
|---|---|---|
| Unit | xUnit, flutter_test, vitest | State machines, ETA propagation, precedence, validation, permissions |
| Integration | Testcontainers (PostgreSQL, Redis) | Endpoints, authorisation matrix, outbox, webhooks |
| Contract | Consumer-driven | SmartRail ↔ payment provider (fake) |
| E2E | Playwright, Flutter integration_test | The five flows below |
| Non-functional | k6, axe-core, OWASP ZAP | Freshness and performance, accessibility, security baseline |

## The five E2E flows

| # | Flow | Specification | Simulator |
|---|---|---|---|
| E2E-1 | Passenger follows a delayed train | [CF-01](../00-requirements/08-cross-app-flows/cf-01-delay-propagation.md) | SIM-02 |
| E2E-2 | Driver offline and reconciliation | [CF-03](../00-requirements/08-cross-app-flows/cf-03-driver-offline.md) | SIM-09 |
| E2E-3 | Controller cancels; every surface follows | [CF-02](../00-requirements/08-cross-app-flows/cf-02-cancellation.md) | SIM-08 |
| E2E-4 | Buy a ticket, board at the gate, journey cancelled, ticket Affected, refund completes, the gate and the inspector reject it | [CF-09](../00-requirements/08-cross-app-flows/cf-09-purchase.md), [CF-10](../00-requirements/08-cross-app-flows/cf-10-affected-refund.md), [CF-11](../00-requirements/08-cross-app-flows/cf-11-inspection.md); EDG-125, 127, 129, 149, 151 | SIM-19, 20, 21, 22 |
| E2E-5 | Display survives connectivity loss | [CF-06](../00-requirements/08-cross-app-flows/cf-06-degraded-data.md), EDG-080 | — |

Each flow's assertions are already written in its cross-app flow document.

## Testability built into the design

`IClock` everywhere (REQ-NFR-005) · seeded, scriptable simulator using the real ingestion path (REQ-SIM-001,
REQ-SIM-002) · fault injection (REQ-SIM-003) · fake payment provider fault switches · correlation id on every response
(REQ-NFR-004) · non-production scenario reset (REQ-SIM-005).

## Linking tests to requirements

Put the requirement id in the test name or attribute — e.g. `[Trait("Req", "REQ-LIVE-004")]`,
`test('REQ-PAX-002 board shows delta', …)`. `python tools/docs/build_traceability.py` picks it up and reports Must
requirements with no test.

## Planned documents

`test-strategy.md` · `test-scenarios.md` · `defect-log.md` (also on the tracker's Defects sheet) · `evidence/` ·
`quality-assessment.md`
