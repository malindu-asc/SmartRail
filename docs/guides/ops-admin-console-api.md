# Controller, scheduler and administrator API guide

Everything the staff web console and the driver app need from the back end, who may call it, and where each thing the passenger app shows comes from. Spec: [SPEC-031](../../specs/SPEC-031-ops-and-admin-console-api.md). Requirements: [permission matrix](../00-requirements/06-requirements/permission-matrix.md), [MVP roles](../00-requirements/06-requirements/mvp-roles.md), [operations use cases](../00-requirements/07-use-cases/operations.md).

- [1. How to call it](#1-how-to-call-it)
- [2. Roles, permissions and demo accounts](#2-roles-permissions-and-demo-accounts)
- [3. Where every app feature comes from](#3-where-every-app-feature-comes-from)
- [4. Endpoints](#4-endpoints)
- [5. State rules](#5-state-rules)
- [6. Audit log](#6-audit-log)
- [7. Data model](#7-data-model)
- [8. Seeded demo data and a test walk-through](#8-seeded-demo-data-and-a-test-walk-through)
- [9. What is not built](#9-what-is-not-built)
- [10. How it is tested](#10-how-it-is-tested)

## 1. How to call it

| | |
|---|---|
| Base address | `http://localhost:8081` when the stack runs from `docker compose` (the API container) |
| Sign in | Keycloak realm `smartrail`, client `smartrail-web` (console) or `smartrail-mobile`. The access token goes in `Authorization: Bearer <token>` |
| Roles | The token's `roles` claim (realm roles). A station master's `stations` claim lists their stations |
| Explore | `GET /api/me` returns who you are and every permission your roles give you; `GET /api/me/permissions/{permission}` answers one |
| Time | All instants are UTC (`...Z`). Dates are the network's local service date (`yyyy-MM-dd`, Asia/Colombo) |
| JSON | `camelCase`. Enums travel as text |
| Plain text | Notice and message text is plain text. Clients must never render it as markup |

**Errors** are RFC 7807 problem details with a stable `code`:

```json
{ "status": 409, "title": "Conflict", "detail": "The journey changed while you were looking at it.", "code": "Journeys.VersionConflict" }
```

| Status | Meaning |
|---|---|
| 400 | Validation: missing reason, unknown reason code, bad value |
| 401 | No or invalid token |
| 403 | Signed in, but the role lacks the permission; or "a second person must do this" |
| 404 | Not found (another driver's message also answers 404, so it does not leak) |
| 409 | State conflict: stale `expectedVersion`, wrong state, platform in use, rest rule, duplicate |

**Concurrent edits.** Journey actions carry `expectedVersion`, the `version` shown on the board or journey detail. If someone else changed the journey meanwhile, the answer is `409 Journeys.VersionConflict` and the screen reloads.

**Reasons.** Cancel, terminate short, interrupt, platform change, reinstate, assign and unassign, out of service, notice retraction and resolution, incident resolution, handover and the critical-incident declaration all need a reason (a code from `GET /api/ops/reason-codes`, or text). Each writes an audit row.

## 2. Roles, permissions and demo accounts

Authority is split by accountability. The administrator decides who can do what and holds **no** operational permission. The controller cannot edit timetables, assign units or drivers, or manage users. The scheduler cannot cancel a running train or approve their own timetable. A station master only reaches their own stations.

| Permission | Passenger | Driver | Inspector | Controller | Station master | Scheduler | Administrator |
|---|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
| `journey.read` (public / own duty view) | ✓ | own | | ✓ | ✓ | ✓ | |
| `ops.read` (live board and journey detail) | | | | ✓ | own stations | ✓ | |
| `journey.cancel`, `journey.reinstate`, `journey.terminate_short`, `journey.interrupt` | | | | ✓ | | | |
| `journey.terminate_emergency` (Incident Manager bundle) | | | | ✓ | | | |
| `journey.platform.change` | | | | ✓ | | | |
| `journey.stop_event.record`, `journey.delay_reason.record` | | own | | ✓ | | | |
| `journey.data.correct` | | | | ✓ | | | |
| `assistance.request` | | own | | | | | |
| `planning.read` (plans, units, crew, dashboard) | | | | ✓ | | ✓ | |
| `timetable.read` | | | | ✓ | | ✓ | |
| `timetable.draft` | | | | | | ✓ | |
| `timetable.approve` (not the author) | | | | | | ✓ | |
| `journey.vehicle.assign`, `journey.driver.assign` | | | | | | ✓ | |
| `vehicle.availability.set` (Fleet Controller bundle) | | | | | | ✓ | |
| `disruption.create`, `disruption.publish`, `disruption.retract` | | | | ✓ | | | |
| `disruption.create_planned` | | | | | | ✓ | |
| `disruption.approve_network_wide` (a second person) | | | | ✓ | | | |
| `incident.declare`, `incident.declare_critical`, `incident.reassign_owner` | | | | ✓ | | | |
| `message.send_to_driver` | | | | ✓ | | | |
| `message.acknowledge` | | own | | | | | |
| `device.health.read` | | | | ✓ | own | | ✓ |
| `device.provision`, `device.revoke` | | | | | | | ✓ |
| `user.manage`, `role.assign`, `security.settings.manage` | | | | | | | ✓ |
| `audit.read.operational` | | | | ✓ | | | |
| `audit.read.access` | | | | ✓ | | | ✓ |
| `report.read` (punctuality) | | | | ✓ | ✓ | ✓ | |
| `station.info.edit` | | | | | own | | |
| `ticket.purchase`, `ticket.read` | ✓ | | | | | | |
| `ticket.inspect` | | | ✓ | | | | |

`ops.read` is new beside the matrix: `journey.read` is the public and own-duty view and would otherwise let a passenger or a driver open the live board. The code is in `src/BuildingBlocks/SmartRail.Api.Core/Security/Permissions.cs` and is covered by tests (section 10).

**Demo accounts** (Keycloak realm import `infra/keycloak/smartrail-realm.json`; passwords are in the development guide, not here):

| Username | Role | Notes |
|---|---|---|
| `controller`, `controller2` | Network controller | Two, so a network-wide notice has a second approver |
| `scheduler`, `scheduler2` | Scheduler | Two, so a timetable draft has a different approver |
| `admin` | System administrator | |
| `driver`, `driver2`, `driver3` | Driver | The three demo drivers; the demo fleet seed gives each one a trainset |
| `passenger` | Passenger | |

## 3. Where every app feature comes from

Nothing in the apps is invented on the client. This is the chain behind each thing a person sees, and who does it.

### Passenger app

| What the passenger sees | Backend call | Created or changed by | Role and endpoint |
|---|---|---|---|
| Stations, routes, search suggestions | `GET /api/network/stations`, `/routes` | Reference data seeded from the network definition | Not edited in a console yet |
| The timetable (services and times) | `GET /api/network/...` | **Scheduler** drafts a version, a **different scheduler** approves it with an effective date | `POST /api/ops/timetable/versions`, `.../submit`, `.../approve` |
| A journey on today's board | `GET /api/journeys/boards/{station}`, `/search`, `/{id}` | **Generated** every night and on start-up from the timetable version in force. Nobody creates it by hand. A special extra train is the exception | Generator (`Journeys:Generation`); `POST /api/journeys/manual` for a special |
| Who drives it and which unit | shown on the staff board | **Scheduler** rosters a driver (`duties`), assigns unit and driver, marks a unit out of service | `POST /api/ops/duties`, `.../journeys/{id}/assign`, `.../units/{id}/out-of-service` |
| Delay, Live / Stale / No live data, next stop | same journey calls | The **train feed** (`/api/ingest/*`, feed-source key), the **driver app** later, or the simulator | Not a console action; the controller sees the same state on the board |
| "Cancelled" | same journey calls | **Controller** cancels with a reason | `POST /api/ops/journeys/{id}/cancel` |
| "Ends at X" (terminated short) | same | **Controller** | `.../terminate-short` |
| Platform shown, "changed from 2" | same | **Controller** changes the platform; the old one is kept for "changed from" | `.../platform` (check first with `.../platform-check`) |
| Alerts screen: disruptions and planned works | `GET /api/operations/disruptions[/{id}]` | **Controller** publishes (network-wide needs a **second controller**); **scheduler** announces planned works; retraction removes it | `POST /api/ops/disruptions`, `.../approve`, `.../retract`; `POST /api/ops/planned-works` |
| My ticket turns **Affected** | `GET /api/ticketing/tickets` | Automatic. Ticket state is read from the journey, so a cancel or a short termination changes it with no second write | The cancel above; `GET /api/ops/journeys/{id}/impact?action=cancel` shows how many first |
| Food menu at a station | `GET /api/network/stations/{code}/menu` | The **catering provider** (separate Node service). Not a railway console function | Out of scope here |
| Tickets, seats, QR | `/api/ticketing/...` | The passenger | SPEC-022 |
| Account, register, export | `/api/account/...` | The passenger | SPEC-021 |
| Follow a journey, inbox, push | none yet | Planned | SPEC-020 (not built; the demo keeps follows on the phone) |

### Driver app (not built yet; the API side that exists)

| Driver feature | Endpoint | Created by | Notes |
|---|---|---|---|
| Message from the controller, with an acknowledgement | `GET /api/driver/messages`, `POST /api/driver/messages/{id}/acknowledge` | **Controller** `POST /api/ops/messages` | Fetching marks it Delivered. A driver sees only their own |
| Ask for help | `POST /api/driver/assistance` | The driver | Appears on the controller dashboard until handled: `POST /api/ops/assistance/{id}/handle` |
| My duty and which train | not built | **Scheduler** rosters and assigns | SPEC-011 |
| Arrived / departed, delay reason, offline queue | not built (the feed ingest takes events from sources) | The driver | SPEC-011 |

### Platform display and station terminal (kiosk)

| Feature | Source |
|---|---|
| Boards and disruptions | The same public calls as the passenger app |
| The display exists on the register and its health is shown | **Administrator** registers it: `POST /api/admin/devices`; health: `GET /api/admin/devices` (controller and station master can read it too) |

## 4. Endpoints

`Perm` is the single permission the endpoint asks for (section 2 says who holds it). All paths start with `/api`.

### 4.1 Who am I

| Method | Path | Perm | Answer |
|---|---|---|---|
| GET | `/me` | signed in | `{ subject, userName, name, roles[], permissions[], consoles[] }` (`consoles` lists which consoles this person may open) |
| GET | `/me/permissions/{permission}` | signed in | `{ permission, allowed }` |

### 4.2 Administrator

| Method | Path | Perm | Body or query | Notes |
|---|---|---|---|---|
| GET | `/admin/dashboard` | `user.manage` | | Staff counts, failed sign-ins, locked accounts, leavers, devices, access-review countdown, recent access changes, and a "needs attention" list (leaver still active, account locked, device offline or late) |
| GET | `/admin/users` | `user.manage` | `role`, `status` (Active, Locked, Disabled, Leaver), `q`, `station`, `skip`, `take` | Staff accounts from Keycloak |
| GET | `/admin/users/{id}` | `user.manage` | | |
| POST | `/admin/users` | `role.assign` | `{ fullName, email, role, stationCodes[] }` | Invites. A station master needs `stationCodes`. The person sets their own password and two-step code; in development the response carries a temporary password when no email service exists |
| PATCH | `/admin/users/{id}` | `user.manage` | `{ fullName?, email?, role?, stationCodes?, reason? }` | A role change needs a `reason`; you cannot change your own access or remove the last administrator (`Identity.CannotChangeOwnAccess`, `Identity.LastAdministrator`) |
| POST | `/admin/users/{id}/disable` | `user.manage` | `{ reason }` | Sessions end at once; records stay |
| POST | `/admin/users/{id}/enable` | `user.manage` | | |
| POST | `/admin/users/{id}/unlock` | `user.manage` | `{ reason }` | After failed sign-ins |
| GET | `/admin/roles` | `user.manage` | | The role and permission matrix, read only |
| GET | `/admin/devices` | `device.health.read` | `type` (displays, gates, terminals), `health` (Online, Late, Offline, Pending, Revoked), `station`, `q` | Health from the last heartbeat: late after 60 s, offline after 90 s (settings) |
| GET | `/admin/devices/{id}` | `device.health.read` | | |
| POST | `/admin/devices` | `device.provision` | `{ name, type, stationCode, location? }` | `type` is PlatformDisplay, ConcourseBoard, GateScanner or Terminal. Starts Pending until the first heartbeat |
| POST | `/admin/devices/{id}/revoke` | `device.revoke` | `{ reason }` | Taken off the register; the record stays for the audit trail |
| GET | `/admin/security` | `security.settings.manage` | | Sign-in protection, sessions, access review and device thresholds, with the last and next review |
| PUT | `/admin/security` | `security.settings.manage` | all settings | Validated (`Identity.SettingInvalid`). The account-lock numbers are applied to Keycloak |
| POST | `/admin/security/access-review/complete` | `security.settings.manage` | | Records that every account was reviewed |

### 4.3 Controller: the live board and journey actions

Station masters may read the board for their own stations only (a request for another station is 403).

| Method | Path | Perm | Body or query | Notes |
|---|---|---|---|---|
| GET | `/ops/dashboard` | `planning.read` | | `kpis` (running, on-time %, delayed, delayed over 15 min, cancelled, no live data, disruptions active and waiting), `needsAttention[]` (assistance requested, no live data, message not acknowledged, no driver, no unit, unit out of service, major delay, interrupted, approval needed, disruption needs review), `devices`, `activeDisruptions` |
| GET | `/ops/journeys` | `ops.read` | `date`, `route`, `station`, `status`, `attention`, `q` | The live board. Each row has `version`, `data` (Live, Stale, Unknown, Scheduled, Confirmed), `delayMinutes`, driver, unit, `needsAttention` and why |
| GET | `/ops/journeys/{id}` | `ops.read` | | Stops (scheduled, actual or expected, platform, source), crew, and `actions[]` with `allowed` and `whyNot` |
| GET | `/ops/journeys/{id}/events` | `ops.read` | `take` | Event history with the source of each event |
| GET | `/ops/journeys/{id}/impact` | `planning.read` | `action` (cancel, terminate_short, platform, interrupt), `stopSequence` | Tickets and travellers affected, displays that will change, the driver who will be told. `followers` is `null` until SPEC-020 exists, never a fake 0 |
| GET | `/ops/reason-codes` | `ops.read` | | Reason lists for cancel, terminate short, interrupt and platform |
| GET | `/ops/routes`, `/ops/routes/{key}` | `ops.read` / `planning.read` | `date` | Routes with the worst state of their journeys; one route with its journeys and assignments (key like `CEN-RIV`) |
| POST | `/ops/journeys/{id}/cancel` | `journey.cancel` | `{ reasonCode, note?, passengerMessage?, expectedVersion }` | Planned, Ready or Interrupted journeys only. Its tickets become Affected. A running train cannot be cancelled outright: end it with terminate short |
| POST | `/ops/journeys/{id}/terminate-short` | `journey.terminate_short` | `{ stopSequence, reasonCode, note?, expectedVersion }` | Ends at a stop the train has not passed; later stops are cancelled (`Journeys.CannotTerminateThere`) |
| POST | `/ops/journeys/{id}/interrupt` | `journey.interrupt` | `{ reasonCode, note?, expectedVersion }` | Running only; no estimate while interrupted |
| POST | `/ops/journeys/{id}/resume` | `journey.interrupt` | `{ expectedVersion }` | |
| POST | `/ops/journeys/{id}/reinstate` | `journey.reinstate` | `{ reason, expectedVersion }` | Before the scheduled departure only (`Journeys.ReinstateTooLate`) |
| GET | `/ops/journeys/{id}/platform-check` | `journey.platform.change` | `stopSequence`, `platform` | Lists trains that would share the platform |
| POST | `/ops/journeys/{id}/platform` | `journey.platform.change` | `{ stopSequence, platform, reasonCode, note?, confirmOverlap, expectedVersion }` | Refused with `Journeys.PlatformInUse` unless `confirmOverlap` is set. The original platform is kept |
| GET | `/ops/reports/punctuality` | `report.read` | `from`, `to`, `route` | On-time % by day and route, average delay, cancellations. No live data is never counted as on time |
| GET | `/ops/reports/punctuality.csv` | `report.read` | same | CSV download |

### 4.4 Controller: disruptions

| Method | Path | Perm | Body or query | Notes |
|---|---|---|---|---|
| GET | `/ops/disruptions` | `disruption.create` | `tab` (active by default, awaiting, drafts, resolved, planned, all) | `counts` and `items`; an item past its review time is flagged expired |
| GET | `/ops/disruptions/{id}` | `disruption.create` | | History and the actions this person may take (an author cannot approve) |
| POST | `/ops/disruptions/preview` | `disruption.create` | `{ title, message, severity }` | How passengers will see it |
| POST | `/ops/disruptions` | `disruption.create` | `{ title, message, severity, networkWide, stationCodes[], type?, startsAtUtc?, expectedEndUtc?, reviewByUtc, submit }` | `submit: false` saves a draft. A local notice publishes at once. A network-wide notice becomes **AwaitingApproval** |
| PUT | `/ops/disruptions/{id}` | `disruption.create` | same | A draft, a rejected notice, or a live **local** notice. A live network-wide notice cannot be edited: retract and write again (`Operations.NotEditable`) |
| POST | `/ops/disruptions/{id}/submit` | `disruption.create` | | Sends a draft or a rejected notice |
| POST | `/ops/disruptions/{id}/approve` | `disruption.approve_network_wide` | | A controller who is not the author (`Operations.SecondApproverRequired`) |
| POST | `/ops/disruptions/{id}/reject` | `disruption.approve_network_wide` | `{ comment }` | The comment is required; the author can fix it and send it again |
| POST | `/ops/disruptions/{id}/resolve` | `disruption.publish` | `{ reason? }` | It is over |
| POST | `/ops/disruptions/{id}/retract` | `disruption.retract` | `{ reason }` | Wrong; disappears from passenger clients within 5 seconds |

Planned works (the scheduler, REQ-OPS-016):

| Method | Path | Perm | Notes |
|---|---|---|---|
| GET | `/ops/planned-works` | `disruption.create_planned` | Planned works that have not started |
| POST | `/ops/planned-works` | `disruption.create_planned` | Same body as a notice, `startsAtUtc` required. Passengers see it in advance. A network-wide one still waits for a controller |

### 4.5 Controller: incidents and driver messages

| Method | Path | Perm | Body | Notes |
|---|---|---|---|---|
| GET | `/ops/incidents` | `incident.declare` | `openOnly` | |
| GET | `/ops/incidents/{id}` | `incident.declare` | | Timeline, linked disruption, messages told |
| GET | `/ops/incidents/sign-out-check` | `incident.declare` | | `mustHandOver` is true while the signed-in controller still owns an open incident |
| POST | `/ops/incidents` | `incident.declare` | `{ title, severity, stationCodes[], reportedBy?, disruptionId?, confirm, reason }` | You become the owner. **Critical** needs `incident.declare_critical`, `confirm: true` and a reason |
| POST | `/ops/incidents/{id}/notes` | `incident.declare` | `{ kind (Note or Action), text, detail? }` | |
| POST | `/ops/incidents/{id}/link-disruption` | `incident.declare` | `{ disruptionId }` | |
| POST | `/ops/incidents/{id}/handover` | `incident.reassign_owner` | `{ newOwnerSubject, note }` | Never left without an owner |
| POST | `/ops/incidents/{id}/resolve` | `incident.declare` | `{ note }` | |
| POST | `/ops/incidents/{id}/emergency-termination` | `journey.terminate_emergency` | `{ journeyId, confirm, reason }` | Terminates the train and raises the incident to Critical. Passengers see only the approved wording |
| GET | `/ops/message-templates` | `message.send_to_driver` | | |
| GET | `/ops/messages` | `message.send_to_driver` | `openOnly` | Sent, Delivered, Acknowledged; overdue ones are flagged |
| GET | `/ops/messages/{id}` | `message.send_to_driver` | | |
| POST | `/ops/messages` | `message.send_to_driver` | `{ driverSubject? or journeyId?, incidentId?, template?, body, requiresAck, validUntilUtc }` | A journey id goes to that journey's driver |
| POST | `/ops/messages/{id}/resend` | `message.send_to_driver` | `{ validForMinutes }` | |
| GET | `/ops/assistance` | `message.send_to_driver` | | Open requests, oldest first |
| POST | `/ops/assistance/{id}/handle` | `message.send_to_driver` | `{ outcome? }` | |

### 4.6 Scheduler: timetable, units and crew

| Method | Path | Perm | Body or query | Notes |
|---|---|---|---|---|
| GET | `/ops/timetable/versions` | `timetable.read` | | Drafts and in review first; the version in force is marked `inForce` |
| GET | `/ops/timetable/versions/{id}` | `timetable.read` | | Services and stops, and `actions` (`canEdit`, `canSubmit`, `canApprove`, why not) |
| GET | `/ops/timetable/versions/{id}/compare` | `timetable.read` | `against` | Added, removed and changed services against the version in force |
| POST | `/ops/timetable/versions` | `timetable.draft` | `{ label }` | A copy of the version in force. One open draft at a time (`Network.DraftAlreadyOpen`) |
| POST | `/ops/timetable/versions/{id}/services` | `timetable.draft` | `{ trainNumber, routeCode, calendarCode, direction, category, stops[{ stationCode, arrival?, departure? }] }` | Times are `HH:mm`; hours may pass 24 |
| PUT | `/ops/timetable/versions/{id}/services/{serviceId}/times` | `timetable.draft` | `{ stops[{ stationCode, arrival?, departure? }] }` | Same stops, new times |
| DELETE | `/ops/timetable/versions/{id}/services/{serviceId}` | `timetable.draft` | | |
| POST | `/ops/timetable/versions/{id}/submit` | `timetable.draft` | | Draft to InReview |
| POST | `/ops/timetable/versions/{id}/approve` | `timetable.approve` | `{ effectiveFrom }` | Not the author (`Network.AuthorCannotApprove`), a future date (`Network.EffectiveDateInPast`), a date no other version uses. The old version becomes Superseded. Journeys already generated are untouched |
| GET | `/ops/planning/needs` | `planning.read` | `date` | Journeys with no driver or unit, with suggestions, and units going out of service |
| GET | `/ops/planning/assignments` | `planning.read` | `date` | By unit |
| GET | `/ops/units` | `planning.read` | `date` | Units, Available or OutOfService, with journeys |
| GET | `/ops/units/{id}/impact` | `planning.read` | | Journeys that lose the unit and who could take over |
| POST | `/ops/units/{id}/out-of-service` | `vehicle.availability.set` | `{ fromUtc, toUtc, reason }` | Returns the affected journeys with suggestions |
| DELETE | `/ops/units/{id}/out-of-service/{periodId}` | `vehicle.availability.set` | | Back in service |
| POST | `/ops/units/{id}/apply-suggestions` | `journey.vehicle.assign` | `{ assignments?[{ journeyId, unitId }], reason }` | One audited change each |
| GET | `/ops/crew` | `planning.read` | `date` | Driver duties |
| POST | `/ops/duties` | `journey.driver.assign` | `{ driverSubject, fromUtc, toUtc }` | Rosters a driver for a shift |
| GET | `/ops/journeys/{id}/assignment-options` | `planning.read` | | Drivers and units that could take it, each with the checks (rest, overlap, availability) |
| POST | `/ops/journeys/{id}/assign` | vehicle and/or driver assign | `{ unitId?, dutyId?, reason, acceptRestConflict, expectedVersion }` | A unit needs `journey.vehicle.assign`, a driver `journey.driver.assign`. Checks overlap, rest (`Journeys.DriverNotRested`), availability |
| POST | `/ops/journeys/{id}/unassign` | `journey.driver.assign` | `{ unit, driver, reason, expectedVersion }` | |

### 4.7 The driver's side

| Method | Path | Perm | Notes |
|---|---|---|---|
| GET | `/driver/messages` | `message.acknowledge` | Own messages; fetching marks them Delivered |
| POST | `/driver/messages/{id}/acknowledge` | `message.acknowledge` | Another driver's message answers 404 |
| POST | `/driver/assistance` | `assistance.request` | `{ journeyId?, stationCode?, note? }` |

### 4.8 Audit

| Method | Path | Perm | Query |
|---|---|---|---|
| GET | `/audit` | signed in, then by log | `kind` (operational or access), `from`, `to`, `actor`, `action`, `q`, `take` (max 500) |

The controller reads the operational log (journeys, notices, incidents, messages, timetable) and the access log. The administrator reads the access log only; asking for the operational log is 403. Anyone else is 403. Rows are read only: the database refuses updates and deletes.

### 4.9 Public reads that the consoles feed

| Method | Path | Notes |
|---|---|---|
| GET | `/operations/disruptions`, `/operations/disruptions/{id}` | Published and planned notices, cached for 5 seconds |
| GET | `/journeys/boards/{station}`, `/journeys/search`, `/journeys/{id}` | Boards and journeys |
| POST | `/journeys/manual` | A special or relief train that is not in the timetable (scheduler only), with a reason |
| POST | `/ingest/stop-events`, `/ingest/positions` | Feed sources, with a feed key |

## 5. State rules

**Journey** (`lifecycle`): Planned → Ready → Running → Completed. From Planned or Ready: Cancelled (reinstate returns it to Planned before departure). From Running: Interrupted (resume returns it), TerminatedShort, TerminatedEmergency. An Interrupted journey can be cancelled. `actions[]` on the journey detail says which are allowed and why not.

**Disruption:** Draft → (submit) → Published, or AwaitingApproval when network-wide. AwaitingApproval → Published (a different controller) or Rejected (with a comment) → submit again. Published → Resolved or Retracted. Resolved and Retracted are final. A published notice past its review time is "expired - review", not removed.

**Incident:** Open → Resolved, with a timeline (Note, Action, Handover, System). Owner always exists; handover needs a note.

**Driver message:** Sent → Delivered (the driver's app fetched it) → Acknowledged. Overdue when acknowledgement is required and not received after 5 minutes.

**Timetable version:** Draft → InReview → Published → Superseded. Draft and InReview can change; Published and Superseded never do. A published version is corrected by publishing a new one.

**Device:** Pending → Active (first heartbeat) → Revoked. Health: Online, Late, Offline.

## 6. Audit log

Every row records who (subject and role), when, what (`action`), which thing (`targetType`, `targetId`), the reason, and before and after where it applies.

| Log | Actions |
|---|---|
| Journeys | `journey.cancel`, `journey.terminate_short`, `journey.interrupt`, `journey.resume`, `journey.reinstate`, `journey.platform.change`, `journey.terminate_emergency`, `journey.assign`, `journey.unassign`, `journey.manual.create`, `duty.create`, `unit.out_of_service`, `unit.back_in_service` |
| Operations | `disruption.draft`, `.submit`, `.publish`, `.update`, `.approve`, `.reject`, `.resolve`, `.retract`; `incident.declare`, `.handover`, `.resolve`, `.link_disruption`, `.emergency_termination`; `message.send`, `.resend`, `.acknowledge`; `assistance.request`, `.handle` |
| Network | `timetable.draft`, `timetable.service.add`, `.service.remove`, `.service.times`, `timetable.submit`, `timetable.publish` |
| Identity (access) | `user.invite`, `.role.change`, `.update`, `.disable`, `.enable`, `.unlock`; `device.register`, `.revoke`; `security.settings.update`, `access_review.complete` |

## 7. Data model

One schema per module, snake_case. Cross-module data goes through Contracts only (checked by architecture tests).

| Schema | Tables added for the consoles |
|---|---|
| `identity` | `devices` (name, type, station, status, last heartbeat, software version, registered and revoked by), `settings` (security settings as key and value), `audit_entries` |
| `journeys` | `crew_duties`, `vehicle_unavailability` (period with an exclusion constraint), journey columns for cancel and end reasons, platform history and `version`; `audit_entries` |
| `operations` | `disruptions` (adds type, start time, submitted, rejected by and comment), `incidents`, `incident_entries`, `control_messages`, `assistance_requests`, `audit_entries` |
| `network` | `timetable_versions` (already there; now driven by the workflow), `audit_entries` |

Each `audit_entries` table is append-only through a PostgreSQL trigger.

## 8. Seeded demo data and a test walk-through

The base demo seed is small: the fictional network and timetable, eight trainsets, three drivers, and today's journeys assigned to them (`Journeys:Seed:DemoFleet`). Everything else (users, notices, driver messages, assignments for the rest of the week) is created through the apps.

Optionally, `Demo:Scenarios:Enabled=true` (off by default, not used in the demo) adds test data for trying every screen. The API then seeds on start:

- Trains in many states (finished early and late, running on time and 12 minutes late, one with no live data, cancelled, platform changes) with tickets on them.
- Notices: five published (one of them network-wide), one resolved, one **waiting for approval** (written by `controller2`, so `controller` can approve it), **planned works** (starts in two days, by the scheduler), and a **draft**.
- Incidents: one open (Signal failure at Mill Lane, owned by `controller`) and one resolved.
- Driver messages for the demo driver: one **not acknowledged yet** (shows on the dashboard) and one acknowledged. One open **assistance request**.
- Eight devices with a spread of Online, Late, Offline and Pending.

A quick pass through the whole chain (sign in as each role in the console, or call the API with a token):

1. `admin`: `GET /api/admin/dashboard`, invite a second scheduler, check `GET /api/audit?kind=access`.
2. `scheduler`: `POST /api/ops/timetable/versions` (label it), change one service's times, submit.
3. The second scheduler: `POST .../approve` with a future `effectiveFrom`. The author trying the same gets 403.
4. `controller`: `GET /api/ops/dashboard`; cancel a journey that has not left (`POST /api/ops/journeys/{id}/cancel`); see in the passenger app that the train is Cancelled and a ticket on it is Affected.
5. `controller`: change a platform; the app shows "changed from".
6. `controller2`: approve the waiting network-wide notice; it appears on the Alerts screen.
7. `controller`: send the driver a message; as `driver`, `GET /api/driver/messages` and acknowledge it; the dashboard item disappears.
8. `controller`: `GET /api/audit?kind=operational` shows everything above with the actor.

## 9. What is not built

| Gap | Why and where it is owned |
|---|---|
| Server-side **follow**, inbox and push | SPEC-020. The impact preview says `followers: null` |
| **Station master** console: own-station notices, lift, stair and gate status, scan log | SPEC-028. The role exists and can read its own stations' board |
| **Driver duty, stop events, delay reasons, offline queue** | SPEC-011. Only messages, acknowledgement and assistance exist |
| **Device sign-in, pairing and heartbeat intake** | Decided out of scope. The register and health view exist; heartbeats are seeded or recorded by a later service |
| **Data correction** of recorded events (UC-OPS-14) and ghost journeys | Not scheduled |
| **Calendar editing** in a timetable draft | Services and times can be edited; days of the week live in shared calendars |
| **Customer service** role and refunds view | Deferred role; refunds are SPEC-024 |
| Station-scoped dashboard | The dashboard is network-wide, so it is for the controller and the scheduler only |

Points to settle with the owner: the wireframe shows planning assignments inside the controller screens, but the permission matrix gives vehicle and driver assignment to the scheduler only, so the controller has read access (`planning.read`) and no assign action. The documents say a running train is terminated short and the rest cancelled, so the console's **Cancel** button must be offered only for trains that have not left; for a running train it offers **Terminate short** (the journey detail's `actions[]` says which is allowed).

## 10. How it is tested

- `tests/SmartRail.Modules.Operations.Tests`: notice, incident, message and assistance rules; the permission matrix (the administrator holds no operational permission, the scheduler cannot cancel, the controller cannot plan, the console board is staff only).
- `tests/SmartRail.Modules.Network.Tests`: timetable version edits and approval rules.
- `tests/SmartRail.Api.IntegrationTests` (real PostgreSQL in a container, migrations applied, signed test tokens):
  - every console endpoint answers 401 without a token and 403 for each role that lacks its permission;
  - controller journey actions with reason and version, impact, audit;
  - the network-wide notice: wait, author refused, second controller approves, public list, retract;
  - incidents, the sign-out check, critical needing confirmation; driver messages in their own scope; assistance on the dashboard;
  - the timetable draft, compare, submit, refusal of the author, approval, supersession;
  - the administrator's staff, device and security flows and the access log;
  - the demo seed on a fresh database, run twice.
- `tests/SmartRail.ArchitectureTests`: module boundaries.

Run them all with `dotnet test SmartRail.slnx -c Release` (Docker must be running for the integration tests).
