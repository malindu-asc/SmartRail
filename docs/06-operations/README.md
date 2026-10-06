# Observability and operations

**Brief:** §19 — the organisation must be able to operate the platform in production. **Status:** Planned —
dashboards and runbook on D18.

## Requirements this phase delivers

| Requirement | Meaning |
|---|---|
| REQ-NFR-004 | Structured logs with correlation ids; traces spanning SmartRail and RailBite; health endpoints; metrics |
| REQ-NFR-001 | Freshness: accepted update → clients ≤ 5 s p95 |
| REQ-DSP-005 | Device heartbeat; offline after 90 s |
| REQ-DATA-002 | No tokens, emails or driver coordinates in logs |
| REQ-DATA-003 | Retention schedule and legal hold |

## Metrics that matter

Telemetry ingest lag · accepted / rejected updates by reason · ETA recompute time · journeys in Unknown · connected
realtime clients · push success and failure · outbox depth and dead-letter count · devices online / offline ·
API error rate.

## Alerts (defined even if only logged in the demo environment)

| Alert | Threshold |
|---|---|
| Telemetry ingest lag | > 60 s |
| Journeys Unknown | more than 10 % of running journeys |
| Dead-letter queue | non-empty |
| Devices offline | more than 10 % at any station |
| API error rate | > 2 % over 5 min |

## Planned documents

`runbook.md` (top five incidents with steps) · `slo.md` · `deployment.md` · `data-retention.md` · `dashboards/`
