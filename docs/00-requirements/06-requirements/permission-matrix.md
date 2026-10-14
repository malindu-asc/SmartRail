# Permission matrix

Brief §9: users "should not simply be treated as a single Administrator role". Authorisation is
**permission-based**: roles are bundles of permissions, and some permissions are **scoped** (`:own` — own station,
own duty). The application layer checks scope, not just role (REQ-OPS-001, ADR-0011).

**Separation of duties** is the concrete meaning of "not one admin": the System Administrator manages *who can
do what* and holds **no** operational permission. One compromised admin account cannot run the railway.

**Testing rule:** every ✓ cell has a positive test and every ✗ in a row with at least one ✓ has a negative test
(REQ-SEC-003). **MVP:** seven roles have accounts, demo users and full negative coverage — NC, SM, SCH, SA, DRV, TI and the passenger (ASM-013, ASM-045; see [mvp-roles.md](mvp-roles.md)). IM and RA below are **capability bundles** held by NC, FC is held by SCH, and CS is deferred.

## Roles

| Code | Role | In the MVP | Posture |
|---|---|---|---|
| NC | Network Controller | ✓ account (two demo users) | Staff |
| SCH | Scheduler | ✓ account | Staff |
| SM | Station Master | ✓ account (two demo users, different stations) | Staff (station-scoped) |
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
| journey.platform.change | ✓ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
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
| disruption.create | ✓ | ✓ planned only | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| disruption.publish (scope ≤ route) | ✓ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| disruption.approve_network_wide *(second person)* | ✓ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| disruption.retract | ✓ | ✗ | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
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
- **Scoped permissions in the MVP** are demonstrated by the Driver (`own` duty), the Passenger (`own` tickets) and the **Station Master (`own` station)**: reading another driver's duty, another passenger's ticket, or editing another station must fail even when the request is otherwise valid.
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

## Station and gate permissions (added 2026-10-07)

**SM** is the Station Master (own station only). **DEV(gate)** is a gate scanner device. The earlier station-manager cells for platform
changes and disruptions were changed to ✗: the Station Master sees them, but decisions stay with the Network Controller.

| Permission | PAX | TI | NC | SM | SCH | SA | DRV | DEV(gate) |
|---|---|---|---|---|---|---|---|---|
| scan.board / scan.exit (send a scan) | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ own station |
| scan.inspect (send an inspection scan) | ✗ | ✓ | ✗ | ✗ | ✗ | ✗ | ✗ | ✗ |
| scan.log.read | ✗ | own scans | ✓ all | own station | ✗ | ✗ | ✗ | ✗ |
| excess_fare.mark_collected | ✗ | ✗ | ✗ | own station | ✗ | ✗ | ✗ | ✗ |
| station.notice.publish | ✗ | ✗ | ✗ | own station | ✗ | ✗ | ✗ | ✗ |
| station.access_status.set (lift, stair, gate) | ✗ | ✗ | ✗ | own station | ✗ | ✗ | ✗ | ✗ |
| device.health.read | ✗ | ✗ | ✓ all | own station | ✗ | ✓ all | ✗ | ✗ |
| gate.provision / revoke | ✗ | ✗ | ✗ | ✗ | ✗ | ✓ | ✗ | ✗ |

**Negative tests that matter:** a Station Master at one station reading another's scan log, editing its notices or lift status, or
seeing its devices; a Station Master cancelling a journey or publishing a disruption; a gate device token calling any endpoint
other than scan; an inspector marking an excess fare collected.
