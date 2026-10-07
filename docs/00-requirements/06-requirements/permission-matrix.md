# Permission matrix

Brief §9: users "should not simply be treated as a single Administrator role". Authorisation is
**permission-based**: roles are bundles of permissions, and some permissions are **scoped** (`:own` — own station,
own duty). The application layer checks scope, not just role (REQ-OPS-001, ADR-0011).

**Separation of duties** is the concrete meaning of "not one admin": the System Administrator manages *who can
do what* and holds **no** operational permission. One compromised admin account cannot run the railway.

**Testing rule:** every ✓ cell has a positive test and every ✗ in a row with at least one ✓ has a negative test
(REQ-SEC-003). **MVP:** six roles have accounts, demo users and full negative coverage — NC, SCH, SA, DRV, TI and the passenger (ASM-013, ASM-045; see [mvp-roles.md](mvp-roles.md)). SM, IM, CS, FC and RA below are **capability bundles**: kept so the design stays complete, but held by NC or SCH in the MVP (CS is deferred).

## Roles

| Code | Role | In the MVP | Posture |
|---|---|---|---|
| NC | Network Controller | ✓ account (two demo users) | Staff |
| SCH | Scheduler | ✓ account | Staff |
| SM | Station Manager | capability of NC | Staff (station-scoped in production) |
| IM | Incident Manager | capability of NC | Staff |
| CS | Customer Service Agent | deferred | Staff |
| FC | Fleet Controller | capability of SCH | Staff |
| RA | Reporting Analyst | capability of NC | Staff |
| SA | System Administrator | ✓ account | Staff |
| DRV | Driver | ✓ account | Staff (duty-scoped) |
| DEV | Display / terminal device | — | Device token |
| FEED | Train detection feed source | — | Source key |
| PUB | Anonymous public / passenger device | — | None / device subscription |

## Matrix

`✓` allowed · `own` allowed within own scope only · `✗` denied · `—` not applicable

### Journeys and running

| Permission | NC | SCH | SM | IM | CS | FC | RA | SA | DRV | DEV | FEED | PUB |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| journey.read | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✗ | own | ✓ | ✗ | ✓ public view |
| journey.cancel | ✓ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| journey.reinstate | ✓ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| journey.terminate_short | ✓ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| journey.terminate_emergency | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| journey.platform.change | ✓ | ✗ | own | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| journey.vehicle.assign | ✗ | ✓ | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| journey.driver.assign | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| journey.stop_event.record | ✓ correction | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | own | ✗ | ✓ | ✗ |
| journey.delay_reason.record | ✓ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | own | ✗ | ✗ | ✗ |
| journey.data.correct | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| telemetry.ingest | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ | ✗ |
| assistance.request | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | own | ✗ | ✗ | ✗ |

### Timetable, fleet, stations

| Permission | NC | SCH | SM | IM | CS | FC | RA | SA | DRV | DEV | FEED | PUB |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| timetable.read | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✓ | ✗ | ✗ | ✗ | ✗ | ✓ public |
| timetable.draft | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| timetable.approve *(not the author)* | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| vehicle.availability.set | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| station.info.edit | ✗ | ✗ | own | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |

### Disruptions and incidents

| Permission | NC | SCH | SM | IM | CS | FC | RA | SA | DRV | DEV | FEED | PUB |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| disruption.create | ✓ | ✓ planned only | own station | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| disruption.publish (scope ≤ route) | ✓ | ✗ | own station | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| disruption.approve_network_wide *(second person)* | ✓ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| disruption.retract | ✓ | ✗ | own station | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| incident.declare (Minor, Major) | ✓ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| incident.declare (Critical) | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| incident.reassign_owner | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| message.send_to_driver | ✓ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| message.acknowledge | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | own | ✗ | ✗ | ✗ |

### Administration, devices, data

| Permission | NC | SCH | SM | IM | CS | FC | RA | SA | DRV | DEV | FEED | PUB |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| user.manage / role.assign | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| device.provision / revoke | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| device.health.read | ✓ | ✗ | own station | ✗ | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ |
| device.heartbeat | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ |
| audit.read (operational) | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ |
| audit.read (access changes) | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ | ✓ | ✗ | ✗ | ✗ | ✗ |
| report.read / export | ✓ | ✓ | own station | ✓ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ |
| follow.manage | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | own device |
| breakglass.request *(Could)* | ✓ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |

## Notes on the less obvious cells

- **SA cannot read journeys.** Administration and operations are separated; an administrator troubleshooting
  access does not need running data. If this proves impractical, grant read — never write.
- **Scheduler can create only *planned* disruptions** (engineering works), not live ones.
- **Timetable approval must be by a different person** from the author (REQ-OPS-003). The check is on user, not
  role.
- **Scoped permissions in the MVP** are demonstrated by the Driver (`own` duty) and the Passenger (`own` tickets): reading another
  driver's duty or another passenger's ticket must fail even when the request is otherwise valid. The Station Manager's station-scoped
  permission is kept in the design for production but not built (ASM-045).
- **Driver's `own` scope** is the duty: a driver cannot read another driver's roster by changing an id
  (REQ-DRV-001).
- **FEED** can only ingest. It cannot read anything back.

## Ticketing permissions (added 2026-10-06)

One role is added (ASM-043): **TI** Ticket Inspector. No refund role and no customer-service role exist in the MVP:
failed refunds retry automatically and the Network Controller has a read-only view (ASM-046). **PAX** is an authenticated
passenger acting on **their own** tickets only. The PUB (anonymous) column stays as above.

| Permission | PUB | PAX | TI | NC | SA | DRV |
|---|---|---|---|---|---|---|
| account.register / sign-in | ✓ | ✓ | ✗ | ✗ | ✗ | ✗ |
| ticket.purchase | ✗ | own | ✗ | ✗ | ✗ | ✗ |
| ticket.read | ✗ | own | ✗ (via scan result only) | ✗ | ✗ | ✗ |
| refund.request | ✗ | own | ✗ | ✗ | ✗ | ✗ |
| refund.read | ✗ | own | ✗ | ✓ read-only | ✗ | ✗ |
| refund.retry | automatic (system) | own (re-request, idempotent) | ✗ | ✗ | ✗ | ✗ |
| ticket.inspect (validate a QR) | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ (unless also TI) |
| inspection.log.read | ✗ | ✗ | own | ✗ | ✗ | ✗ |
| fare.manage *(seed data in the MVP)* | ✗ | ✗ | ✗ | ✗ | ✓ | ✗ |

**Negative tests that matter:** a passenger reading or refunding **another passenger's** ticket (IDOR); a driver token calling
`ticket.inspect`; a network controller refunding or inspecting; the system administrator refunding or inspecting; an anonymous call to
any ticket endpoint.
