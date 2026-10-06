# Security

**Brief:** §17 (security throughout the lifecycle, not only at the end), §23 Security (threat model, decisions,
risks, controls, evidence of testing). **Status:** Planned — threat model on D5, testing on D17.

## Decisions already made in requirements

| Decision | Requirement |
|---|---|
| Four authentication postures: anonymous public, passenger device, staff, unattended device | REQ-SEC-001 |
| Permission-based authorisation with scoped permissions; separation of duties for administrators | REQ-OPS-001, [permission-matrix.md](../00-requirements/06-requirements/permission-matrix.md) |
| A positive and a negative test for every permission cell, including IDOR | REQ-SEC-003 |
| Public endpoints rate-limited and cached | REQ-SEC-002 |
| Per-source authentication for the train detection feed; unknown sources rejected | REQ-SEC-004 |
| Disruption text plain, length-limited, escaped | REQ-SEC-005 |
| Device tokens per display or terminal, revocable | REQ-DSP-003 |
| HMAC-signed webhooks with replay window and idempotency | REQ-INT-003 |
| Append-only audit for high-consequence actions | REQ-OPS-011 |
| Lockout and MFA for highest-consequence staff roles | REQ-SEC-006 |
| No secrets in the repository; rotatable keys | DF sec-keys |
| SmartRail is an information system, not a safety system | ASM-009 |

## Trust boundaries for the threat model (STRIDE)

| Boundary | Main concerns |
|---|---|
| Public internet → public read API | Scraping, floods, enumeration |
| Passenger device → follow API | Token theft, IDOR on another device's follows |
| Display / terminal → device API | Device impersonation, kiosk escape |
| Driver device → driver API | Lost device, reading another duty, replay |
| Feed source → ingestion API | **Forged train positions reaching every screen** |
| Staff → ops API | Privilege escalation, cross-station edits, administrators operating trains |
| SmartRail ↔ RailBite | Webhook forgery and replay, data over-sharing |

## Planned documents

`threat-model.md` · `security-decisions.md` · `risks.md` · `evidence/` (authorisation negative-suite results, ZAP
baseline, dependency and secret scans)
