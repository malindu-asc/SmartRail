# Data classification and privacy

Brief §20: identify what the platform collects, classify it, and document the important privacy decisions
(REQ-DATA-001).

## Classes

| Class | Definition | Default handling |
|---|---|---|
| **Public** | Intended for anyone | Cacheable, anonymous access |
| **Operational** | Internal running data, not personal | Staff access by role |
| **Personal** | Relates to an identifiable person | Minimised, purpose-limited, deletable |
| **Sensitive-operational** | Operational data that is also about a person, or security-relevant | Strictest access, short retention, never logged |

## Inventory

| Data | Class | Owner module | Retention (ASM-027) | In logs? | Shared with catering? | Who reads |
|---|---|---|---|---|---|---|
| Stations, platforms, facilities | Public | Network & Timetable | Indefinite | Yes | Yes (reference) | Everyone |
| Published timetable | Public | Network & Timetable | Versions kept indefinitely | Yes | Yes (reference) | Everyone |
| Journey state, delays, platforms | Public | Live Operations | 13 months (aggregated) | Yes | ETA and status only | Everyone |
| Disruption notices | Public | Disruptions | 13 months | Yes | No | Everyone |
| Raw telemetry | Operational | Live Operations | **7 days**, unless legal hold | Counts only | No | NC, system |
| Vehicle assignments, availability | Operational | Live Operations | 13 months | Yes | No | SCH, FC, NC |
| Reason codes, data corrections | Operational | Live Operations | 13 months | Yes | No | NC, RA |
| Incident records | Operational | Disruptions | 24 months | Yes | No | IM, NC |
| Audit log | Sensitive-operational | Identity & Access / all | **24 months**, append-only | Its own store | No | IM, RA, SA (access changes) |
| Staff identities, roles | Personal | Identity & Access | Employment + 12 months | ID only, never name | No | SA |
| **Driver duty and location** | **Sensitive-operational** | Live Operations | **7 days** (location), duty 13 months | **Never** coordinates | No | NC; **never public** |
| Driver delay reports | Operational | Live Operations | 13 months | Yes | No | NC, RA |
| Passenger device subscription (push token, followed journeys) | Personal | Notifications | Until deleted; expired after 90 days inactivity | Never token | No | System only |
| Catering order reference | Personal (linked to a device) | Catering Gateway | 90 days | Reference only | Reference only | System only |
| Device tokens (displays) | Sensitive-operational | Identity & Access | Until revoked | Never | No | SA |
| Feed source keys, webhook secrets | Sensitive-operational | Infrastructure | Until rotated | Never | No | Deployment only |

## Decisions that matter

| Decision | Reasoning | Ref |
|---|---|---|
| **No passenger location is collected** | No requirement needs it; station selection is manual or a one-shot coarse lookup | ADR-0013 |
| **No passenger accounts in the MVP** | The only identity-needing feature is following a train; an anonymous device subscription does that with far less personal data | ASM-012 |
| **Driver location only during a duty, never public, 7-day retention** | Train position comes from the infrastructure feed; driver location is employee monitoring and must be limited | REQ-DRV-010, CON-07 |
| **Catering receives no passenger identity** | Order reference, journey reference, collection station and ETA are sufficient | REQ-INT-002 |
| **Logs carry correlation ids, not identities** | Logs are widely accessible and long-lived | REQ-DATA-002 |
| **Legal hold overrides retention** | Investigation after a serious incident needs the raw data | REQ-DATA-003, REQ-OPS-008 |
| **"Indicative information" disclaimer** | Sets expectations; standard operator practice | REQ-PAX-011 |

## Passenger rights in the MVP

- Delete all follows and device data from the app (REQ-PAX-012).
- No account means nothing to export beyond the device's own follows, which the app already shows.
