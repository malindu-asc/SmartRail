# Database schema

**Brief:** §23 data ownership. **Status:** agreed design, checked against a real PostgreSQL 16 (`database-schema.sql` runs clean and its key constraints were exercised). The EF Core migrations are the source of truth once a module has them; any difference is fixed here in the same change.

**Files:** [database-schema.sql](database-schema.sql) is the full DDL (59 tables). This page explains it.

## 1. What the schema covers

Every requirement area that stores data: accounts and devices, the network and timetable, live journeys and ingest, fleet and crew, controller actions, disruptions and incidents, notifications, tickets, payments, refunds and scans. It is designed once, up front, so each spec builds its slice of an agreed whole instead of inventing tables as it goes.

| Schema (module) | Tables | Owns |
|---|---|---|
| `identity` | 6 + audit | Staff profiles, passenger accounts, devices, feed sources, elevated access |
| `network` | 14 + audit | Languages, stations, platforms, access status, notices, routes, timetable versions, calendars, services |
| `journeys` | 13 + audit | Journeys and stops, event history, delay reasons, telemetry, vehicles, assignments, crew duties, punctuality |
| `operations` | 12 + audit | Reason codes, templates, disruptions, incidents, legal holds, controller actions, driver messages, assistance |
| `notifications` | 3 | Device subscriptions, follows, in-app inbox |
| `ticketing` | 11 + audit | Fares, tickets, status history, payments, refunds, scans, excess fares |

Outside this database: **Keycloak** (credentials, role membership, sessions) and **RailBite** catering (its own database, read only through its API).

## 2. Rules that apply everywhere

1. **One schema per module.** A module reads and writes only its own schema.
2. **No foreign keys across modules.** A column that points into another module holds that module's stable code or id (marked `-> module.table` in the SQL). Integrity across modules is checked in the application through Contracts. Foreign keys inside a module are always real. This keeps the modules separable and matches the boundary tests.
3. **Time:** every instant is `timestamptz` in UTC; the server is the clock authority (REQ-NFR-005). A service date is a `date`. Timetable times are minutes after midnight of the service date, so stops past 24:00 work (REQ-LIVE-009).
4. **Identity:** the journey id is the identity of a train run. Train number is a label. Vehicles and crews attach through time-ranged assignments (REQ-LIVE-010, REQ-LIVE-019).
5. **Scheduled times never change.** Expected time and delay are separate columns (state model, REQ-LIVE-002).
6. **Enumerations** are text with `CHECK` constraints and map to C# enums with `HasConversion<string>()`.
7. **Append-only** tables are guarded by a trigger: audit, ticket and refund history, scans, payment callbacks, controller actions, disruption revisions, delay reasons, incident timelines. The event history forbids delete.
8. **Audit:** each module has its own `audit_entries` table, written in the same transaction as the change (the skill's rule), with one shape: actor, role, action, target, reason, before, after, time, correlation id. A read side aggregates them through Contracts.
9. **Optimistic concurrency:** `journeys.version` and `disruptions.version` (REQ-OPS-010).
10. **Integrity in the database, not only the code:** the database refuses a platform from another station, a vehicle or driver double-booking, a self-approved version or network-wide disruption, a Critical incident without confirmation, a second active refund, and a duplicate payment reference.

## 3. Ownership: why journeys own fleet and crew

The identity model assigns vehicles and crew duties to "Live Operations". In our modules that is `journeys`, so fleet, assignments and duties live there. `network` stays the owner of stations, routes and the timetable. This changes one line of the backend skill (module list), updated with this document.

## 4. Module map

### identity
| Table | Purpose | Requirements |
|---|---|---|
| `staff_profiles` | What SmartRail remembers about staff; home station scopes a Station Master | REQ-OPS-001, REQ-OPS-018 |
| `passenger_accounts` | Link to a Keycloak subject; anonymisable | REQ-ACC-001, REQ-ACC-003 |
| `devices` | Displays, terminals, gate scanners; token hash only; heartbeat | REQ-DSP-003, REQ-DSP-005, REQ-GAT-001, REQ-OPS-009 |
| `feed_sources` | Per-source key hash and precedence rank | REQ-SEC-004, source-precedence |
| `elevated_access_grants` | Time-boxed elevated permissions | REQ-OPS-017 |

### network
| Table | Purpose | Requirements |
|---|---|---|
| `languages`, `station_translations` | Two languages, default name on the station | REQ-PAX-013 |
| `stations`, `platforms` | Reference data; platform belongs to its station (alternate key) | REQ-LIVE-014 |
| `station_access_status` | Lifts, stairs, gates working, faulty or closed | REQ-OPS-019 |
| `station_notices` | Local notices by the Station Master | REQ-OPS-018 |
| `routes`, `route_stops` | Ordered lines with running and dwell times | SPEC-002 |
| `timetable_versions` | Draft, InReview, Published, Superseded; self-approval refused in the database | REQ-OPS-003, REQ-LIVE-017 |
| `service_calendars`, `calendar_exceptions` | Days of week, valid range, holiday and special dates | REQ-LIVE-008 |
| `services`, `service_stops` | A run pattern with explicit times; service category (only Regional is sold) | REQ-LIVE-009, REQ-TKT-008 |

### journeys
| Table | Purpose | Requirements |
|---|---|---|
| `journeys` | One run of a service on a service date. Lifecycle, punctuality, confidence kept as separate columns; `service_id` nullable for manual relief services | REQ-LIVE-001..004, 008, 019 |
| `journey_stops` | Scheduled, estimated and actual times per stop; stop state; platform with TBA, Assigned, Changed and its history | REQ-LIVE-014, REQ-PAX-003 |
| `journey_events` | Event history with source, device time, server time, idempotency key, disposition; losers kept | REQ-LIVE-005, 006, REQ-DRV-005, 006, 008, 011 |
| `journey_delay_reasons` | Driver reason kept, controller reclassification appended | source-precedence |
| `telemetry_readings` | Raw feed data, 7 days, accepted or rejected with reason, ghost movements, legal hold | REQ-LIVE-005, 015, REQ-DATA-003 |
| `vehicles`, `vehicle_unavailability` | Fleet and windows | REQ-OPS-012 |
| `vehicle_assignments` | Time-ranged; exclusion constraints stop double booking | REQ-LIVE-010, 011, 018 |
| `crew_duties`, `driver_assignments` | Duty state model; handover as non-overlapping assignments | REQ-DRV-001, 009, UC-OPS-06 |
| `duty_location_samples` | Only while signed on, 7 days | REQ-DRV-010 |
| `punctuality_daily` | Aggregate kept 13 months | REQ-OPS-014 |

### operations
| Table | Purpose | Requirements |
|---|---|---|
| `reason_codes`, `message_templates` | Reference lists; approved text for severe incidents | REQ-OPS-004, REQ-SEC-005, REQ-OPS-008 |
| `disruptions`, `disruption_revisions`, `disruption_affected_journeys` | State model, scope type with matching columns, review-by, network-wide second approver, retraction and correction | REQ-OPS-005, 006, 016, REQ-LIVE-016, REQ-PAX-009 |
| `incidents`, `incident_timeline_entries`, `legal_holds` | Severity, owner, timeline; legal hold overrides purge | REQ-OPS-007, 008, REQ-DATA-003 |
| `journey_actions` | Every controller action with its reason | REQ-OPS-004, REQ-OPS-011 |
| `driver_messages` | Sent, Delivered, Acknowledged with expiry | REQ-OPS-013, REQ-DRV-007 |
| `assistance_requests` | Driver requests, idempotent, urgent when queued offline | UC-DRV-06 |

### notifications
| Table | Purpose | Requirements |
|---|---|---|
| `device_subscriptions` | Anonymous device id and push token (never logged); expires after 90 days idle | REQ-PAX-007, 012 |
| `journey_follows` | Follows and the throttle fields | REQ-PAX-007, REQ-TKT-010 |
| `notifications` | In-app inbox, delivery status with one retry, corrections | REQ-PAX-008, 009, 015 |

Saved stations are kept on the device only; deleting device data removes the subscription, follows and inbox (REQ-PAX-012).

### ticketing
| Table | Purpose | Requirements |
|---|---|---|
| `fare_categories`, `fare_tariffs` | Seeded stage-based fares in LKR; price is computed on the server | REQ-TKT-003 |
| `tickets` | Bound to a journey id, two stops and a service date; QR carries ids only | REQ-TKT-001, 005, 012 |
| `ticket_status_history` | Append-only | REQ-TKT-005 |
| `payment_attempts`, `payment_callbacks` | Idempotent on provider reference; every callback logged and counted; no card data | REQ-PAY-001..003 |
| `refunds`, `refund_status_history` | One active refund per ticket, retry fields | REQ-REF-001..005 |
| `scans`, `excess_fares` | Scan log for gates and inspectors, idempotent per client scan id; excess fare list for the Station Master | REQ-GAT-001..010, REQ-INS-002..006 |

## 5. Retention and purge jobs

| Data | Rule | Job |
|---|---|---|
| `telemetry_readings` | 7 days unless `legal_hold` | Nightly delete by `received_at`, skipping held rows |
| `duty_location_samples` | 7 days | Nightly delete |
| `journey_events`, journeys, assignments | 13 months | Monthly roll-up into `punctuality_daily`, then archive |
| `audit_entries` | 24 months, append-only | Archive, never update |
| `notifications.device_subscriptions` | 90 days idle | Nightly delete (cascades follows and inbox) |
| Pending tickets | 15 minutes | Expiry job on `tickets.expires_at` |
| Tickets, payments, refunds, scans | 24 months and 13 months as per the data classification | Anonymise on account deletion, then archive |
| Passenger account | Anonymised on request | Sets `account_subject` to null on tickets |

## 6. Performance notes

- **Departure board** (REQ-NFR-002, under 300 ms cached): `journey_stops (station_code, scheduled_departure_utc)` partial index excluding passing points, and `journeys` partial index on active lifecycles. Boards are also cached for a few seconds.
- **Confidence** is computed on read from `last_accepted_update_at` with an injectable clock, so a silent feed needs no write to turn Stale or Unknown.
- **Event ingest** deduplicates with partial unique indexes on `(source, sequence)` and on the idempotency key, so a replay is a harmless no-op.
- **Assignments** use `btree_gist` exclusion constraints; the extension is created by the first Journeys migration.
- Redis (ADR 0005) is an optional cache for board snapshots; PostgreSQL stays authoritative.

## 7. How the specs map to the schema

| Spec | Tables it creates |
|---|---|
| SPEC-002 | languages, stations, translations, platforms, routes, route_stops |
| SPEC-003 | timetable_versions, service_calendars, calendar_exceptions, services, service_stops |
| SPEC-004 | journeys, journey_stops, vehicles, vehicle_assignments (seeded), crew_duties, driver_assignments |
| SPEC-005 | journey_events, telemetry_readings, journey_delay_reasons, feed_sources |
| SPEC-007 | punctuality_daily (read model), notification tables |
| SPEC-010 | reason_codes, journey_actions, disruptions, incidents, legal_holds, message_templates |
| SPEC-011 | duty states, duty_location_samples, driver_messages, assistance_requests |
| SPEC-012 | vehicle_unavailability, staff_profiles, elevated_access_grants |
| SPEC-013 | devices, station_access_status, station_notices |
| Ticketing specs | fares, tickets, payments, refunds, scans, excess fares (a `ticketing` module is added) |

A table is created by the migration of the spec that first needs it; the column list comes from this design so nothing is reworked later.

## 8. Open points

- **Ticketing module:** the solution has five modules; `ticketing` is added when its first spec starts.
- **Region of Keycloak ids:** the `subject` columns are 64 characters, enough for a Keycloak UUID.
- **Cross-module integrity jobs:** a nightly check that codes and ids held across modules still resolve is worth adding once Journeys exists.
