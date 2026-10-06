# Permission matrix

Brief §9: users "should not simply be treated as a single Administrator role". Authorisation is
**permission-based**: roles are bundles of permissions, and some permissions are **scoped** (`:own` — own station,
own duty). The application layer checks scope, not just role (REQ-OPS-001, ADR-0011).

**Separation of duties** is the concrete meaning of "not one admin": the System Administrator manages *who can
do what* and holds **no** operational permission. One compromised admin account cannot run the railway.

**Testing rule:** every ✓ cell has a positive test and every ✗ in a row with at least one ✓ has a negative test
(REQ-SEC-003). Six roles have demo users and full negative coverage: NC, SCH, SM, IM, DRV, SA (ASM-013).

## Roles

| Code | Role | Demo user | Posture |
|---|---|---|---|
| NC | Network Controller | ✓ | Staff |
| SCH | Scheduler | ✓ | Staff |
| SM | Station Manager | ✓ | Staff (station-scoped) |
| IM | Incident Manager | ✓ | Staff |
| CS | Customer Service Agent | — | Staff |
| FC | Fleet Controller | — | Staff |
| RA | Reporting Analyst | — | Staff |
| SA | System Administrator | ✓ | Staff |
| DRV | Driver | ✓ | Staff (duty-scoped) |
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
- **Station manager's `own` scope** is the core IDOR test: editing another station's platform or info must fail
  even when the request is otherwise valid.
- **Driver's `own` scope** is the duty: a driver cannot read another driver's roster by changing an id
  (REQ-DRV-001).
- **FEED** can only ingest. It cannot read anything back.
