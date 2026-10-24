-- =====================================================================================
-- SmartRail database schema (PostgreSQL 16+)  -  REVIEW ARTEFACT
--
-- This file is the agreed design of the whole database. The EF Core migrations of each
-- module are the source of truth once they exist; when they differ, fix this file in the
-- same change. Narrative, ownership rules and retention are in database-schema.md.
--
-- Rules (see database-schema.md section 2). Auditable tables use created_at_utc / updated_at_utc
-- (the BaseDbContext audit columns); other timestamps are plain *_at.
--   * One schema per module: identity, network, journeys, operations, notifications, ticketing.
--   * Inside a module: full foreign keys. Across modules: NO foreign keys. A column that points
--     into another module stores that module's stable code or id (marked "-> module.table").
--   * Every instant is timestamptz in UTC. Service dates are `date`. Times of day on a
--     timetable are minutes after midnight of the service date (may exceed 1440).
--   * Enumerations are text with CHECK constraints; C# enums map with HasConversion<string>.
--   * Append-only tables are protected by trigger (common.forbid_update_delete).
--   * Each module has its own outbox_messages (BaseDbContext) and audit_entries (below).
-- =====================================================================================

CREATE EXTENSION IF NOT EXISTS btree_gist;   -- exclusion constraints on time ranges

CREATE SCHEMA IF NOT EXISTS common;
CREATE SCHEMA IF NOT EXISTS identity;
CREATE SCHEMA IF NOT EXISTS network;
CREATE SCHEMA IF NOT EXISTS journeys;
CREATE SCHEMA IF NOT EXISTS operations;
CREATE SCHEMA IF NOT EXISTS notifications;
CREATE SCHEMA IF NOT EXISTS ticketing;

CREATE OR REPLACE FUNCTION common.forbid_update_delete() RETURNS trigger AS $$
BEGIN
    RAISE EXCEPTION '% on %.% is not allowed: the table is append-only', TG_OP, TG_TABLE_SCHEMA, TG_TABLE_NAME;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION common.forbid_delete() RETURNS trigger AS $$
BEGIN
    RAISE EXCEPTION 'DELETE on %.% is not allowed', TG_TABLE_SCHEMA, TG_TABLE_NAME;
END;
$$ LANGUAGE plpgsql;

-- =====================================================================================
-- IDENTITY  (Keycloak owns credentials and role membership; this schema holds what
--            SmartRail itself must remember)
-- =====================================================================================

CREATE TABLE identity.staff_profiles (
    subject            varchar(64)  PRIMARY KEY,                 -- Keycloak "sub"
    display_name       varchar(200) NOT NULL,
    employee_code      varchar(30)  NOT NULL UNIQUE,
    home_station_code  varchar(5),                               -- -> network.stations.code ; scope of a Station Master
    is_active          boolean      NOT NULL DEFAULT true,
    created_at_utc         timestamptz  NOT NULL DEFAULT now(),
    updated_at_utc         timestamptz
);

CREATE TABLE identity.passenger_accounts (                       -- REQ-ACC-001..004; email and password stay in Keycloak
    subject         varchar(64)  PRIMARY KEY,
    display_name    varchar(200),
    status          varchar(12)  NOT NULL DEFAULT 'Active' CHECK (status IN ('Active', 'Anonymised')),
    created_at      timestamptz  NOT NULL DEFAULT now(),
    anonymised_at   timestamptz,
    CHECK ((status = 'Anonymised') = (anonymised_at IS NOT NULL))
);

CREATE TABLE identity.devices (                                  -- displays, terminals, gate scanners (REQ-DSP-003, REQ-GAT-001, REQ-OPS-009)
    id                 uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    device_type        varchar(10)  NOT NULL CHECK (device_type IN ('Display', 'Terminal', 'Gate')),
    name               varchar(100) NOT NULL,
    station_code       varchar(5)   NOT NULL,                    -- -> network.stations.code
    platform_code      varchar(3),
    token_hash         varchar(128) NOT NULL UNIQUE,             -- the token itself is never stored
    status             varchar(10)  NOT NULL DEFAULT 'Pending' CHECK (status IN ('Pending', 'Online', 'Stale', 'Offline', 'Revoked')),
    last_heartbeat_at  timestamptz,
    provisioned_by     varchar(64)  NOT NULL,
    provisioned_at     timestamptz  NOT NULL DEFAULT now(),
    revoked_by         varchar(64),
    revoked_at         timestamptz,
    CHECK ((status = 'Revoked') = (revoked_at IS NOT NULL))
);
CREATE INDEX ix_devices_station ON identity.devices (station_code);

CREATE TABLE identity.feed_sources (                             -- REQ-SEC-004; precedence rank from source-precedence.md
    id               uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    code             varchar(30)  NOT NULL UNIQUE,
    name             varchar(100) NOT NULL,
    key_hash         varchar(128) NOT NULL,
    precedence_rank  smallint     NOT NULL CHECK (precedence_rank >= 1),
    is_active        boolean      NOT NULL DEFAULT true,
    created_at       timestamptz  NOT NULL DEFAULT now(),
    rotated_at       timestamptz,
    last_used_at     timestamptz
);

CREATE TABLE identity.elevated_access_grants (                   -- REQ-OPS-017 (time-boxed, automatic expiry)
    id               uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    user_subject     varchar(64)  NOT NULL REFERENCES identity.staff_profiles (subject),
    permissions      text[]       NOT NULL,
    reason           varchar(500) NOT NULL,
    requested_at     timestamptz  NOT NULL DEFAULT now(),
    expires_at       timestamptz  NOT NULL,
    revoked_at       timestamptz,
    CHECK (expires_at > requested_at)
);

-- =====================================================================================
-- NETWORK  (stations, routes, timetable)   SPEC-002, SPEC-003, SPEC-015
-- =====================================================================================

CREATE TABLE network.languages (
    code        varchar(10)  PRIMARY KEY,
    name        varchar(100) NOT NULL,
    is_default  boolean      NOT NULL
);
CREATE UNIQUE INDEX ux_languages_default ON network.languages (is_default) WHERE is_default;

CREATE TABLE network.stations (
    id          uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    code        varchar(5)   NOT NULL UNIQUE CHECK (code ~ '^[A-Z]{3,5}$'),
    name        varchar(200) NOT NULL,                           -- default-language name
    facilities  text[]       NOT NULL DEFAULT '{}',              -- StepFree, Lifts, Toilets, TicketOffice, AssistancePoint
    is_active   boolean      NOT NULL DEFAULT true,
    created_at_utc  timestamptz  NOT NULL DEFAULT now(),
    updated_at_utc  timestamptz
);
CREATE INDEX ix_stations_name ON network.stations (name);

CREATE TABLE network.station_translations (
    station_id     uuid         NOT NULL REFERENCES network.stations (id) ON DELETE CASCADE,
    language_code  varchar(10)  NOT NULL REFERENCES network.languages (code),
    name           varchar(200) NOT NULL,
    PRIMARY KEY (station_id, language_code)
);

CREATE TABLE network.platforms (
    id            uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    station_id    uuid        NOT NULL REFERENCES network.stations (id) ON DELETE CASCADE,
    code          varchar(3)  NOT NULL,
    is_available  boolean     NOT NULL DEFAULT true,
    UNIQUE (station_id, code),
    UNIQUE (id, station_id)                                      -- target of the "platform belongs to the stop's station" keys
);

CREATE TABLE network.station_access_status (                     -- REQ-OPS-019: lifts, stairs, gates
    id            uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    station_id    uuid         NOT NULL REFERENCES network.stations (id) ON DELETE CASCADE,
    asset_type    varchar(10)  NOT NULL CHECK (asset_type IN ('Lift', 'Stairs', 'Gate')),
    asset_code    varchar(20)  NOT NULL,
    description   varchar(200),
    status        varchar(10)  NOT NULL CHECK (status IN ('Working', 'Faulty', 'Closed')),
    updated_at    timestamptz  NOT NULL DEFAULT now(),
    updated_by    varchar(64)  NOT NULL,
    UNIQUE (station_id, asset_type, asset_code)
);

CREATE TABLE network.station_notices (                           -- REQ-OPS-018: local notices posted by a Station Master
    id            uuid          PRIMARY KEY DEFAULT gen_random_uuid(),
    station_id    uuid          NOT NULL REFERENCES network.stations (id) ON DELETE CASCADE,
    message       varchar(500)  NOT NULL,                        -- plain text (REQ-SEC-005)
    starts_at     timestamptz   NOT NULL,
    ends_at       timestamptz,
    created_by    varchar(64)   NOT NULL,
    created_at    timestamptz   NOT NULL DEFAULT now(),
    withdrawn_at  timestamptz,
    CHECK (ends_at IS NULL OR ends_at > starts_at)
);
CREATE INDEX ix_station_notices_active ON network.station_notices (station_id, starts_at) WHERE withdrawn_at IS NULL;

CREATE TABLE network.routes (
    id          uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    code        varchar(10)  NOT NULL UNIQUE CHECK (code ~ '^[A-Z]{2,10}$'),
    name        varchar(200) NOT NULL,
    is_active   boolean      NOT NULL DEFAULT true,
    created_at_utc  timestamptz  NOT NULL DEFAULT now(),
    updated_at_utc  timestamptz
);

CREATE TABLE network.route_stops (
    route_id              uuid         NOT NULL REFERENCES network.routes (id) ON DELETE CASCADE,
    sequence              int          NOT NULL CHECK (sequence >= 1),
    station_id            uuid         NOT NULL REFERENCES network.stations (id),
    default_platform_id   uuid,
    minutes_from_previous int          NOT NULL CHECK (minutes_from_previous >= 0),
    dwell_minutes         int          NOT NULL CHECK (dwell_minutes >= 0),
    kind                  varchar(20)  NOT NULL CHECK (kind IN ('Stop', 'PickUpOnly', 'SetDownOnly', 'Passing')),
    PRIMARY KEY (route_id, sequence),
    UNIQUE (route_id, station_id),
    FOREIGN KEY (default_platform_id, station_id) REFERENCES network.platforms (id, station_id)
);

CREATE TABLE network.timetable_versions (                        -- REQ-OPS-003, REQ-LIVE-017
    id              uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    label           varchar(100) NOT NULL,
    status          varchar(10)  NOT NULL CHECK (status IN ('Draft', 'InReview', 'Published', 'Superseded')),
    effective_from  date,
    author          varchar(64)  NOT NULL,
    approved_by     varchar(64),
    published_at    timestamptz,
    created_at_utc      timestamptz  NOT NULL DEFAULT now(),
    updated_at_utc      timestamptz,
    CHECK (status IN ('Draft', 'InReview') OR effective_from IS NOT NULL),
    CHECK (status NOT IN ('Published', 'Superseded') OR (approved_by IS NOT NULL AND approved_by <> author))
);
CREATE UNIQUE INDEX ux_timetable_effective ON network.timetable_versions (effective_from)
    WHERE status IN ('Published', 'Superseded');

CREATE TABLE network.service_calendars (
    id          uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    code        varchar(20)  NOT NULL UNIQUE,
    name        varchar(100) NOT NULL,
    monday      boolean      NOT NULL, tuesday  boolean NOT NULL, wednesday boolean NOT NULL,
    thursday    boolean      NOT NULL, friday   boolean NOT NULL, saturday  boolean NOT NULL,
    sunday      boolean      NOT NULL,
    valid_from  date         NOT NULL,
    valid_to    date,
    CHECK (valid_to IS NULL OR valid_to >= valid_from)
);

CREATE TABLE network.calendar_exceptions (                       -- REQ-LIVE-008: holidays and specials
    calendar_id  uuid         NOT NULL REFERENCES network.service_calendars (id) ON DELETE CASCADE,
    date         date         NOT NULL,
    kind         varchar(10)  NOT NULL CHECK (kind IN ('Removed', 'Added')),
    reason       varchar(200),
    PRIMARY KEY (calendar_id, date)
);

CREATE TABLE network.services (
    id                    uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    timetable_version_id  uuid         NOT NULL REFERENCES network.timetable_versions (id),
    route_id              uuid         NOT NULL REFERENCES network.routes (id),
    calendar_id           uuid         NOT NULL REFERENCES network.service_calendars (id),
    train_number          varchar(10)  NOT NULL,                 -- a label, never an identity (REQ-LIVE-019)
    direction             varchar(7)   NOT NULL CHECK (direction IN ('Forward', 'Reverse')),
    service_category      varchar(12)  NOT NULL DEFAULT 'Regional' CHECK (service_category IN ('Regional', 'Intercity', 'HighSpeed')),  -- REQ-TKT-008
    UNIQUE (timetable_version_id, train_number)
);
CREATE INDEX ix_services_route ON network.services (route_id);

CREATE TABLE network.service_stops (
    service_id           uuid         NOT NULL REFERENCES network.services (id) ON DELETE CASCADE,
    sequence             int          NOT NULL CHECK (sequence >= 1),
    station_id           uuid         NOT NULL REFERENCES network.stations (id),
    arrival_minutes      int          CHECK (arrival_minutes BETWEEN 0 AND 2879),     -- minutes after midnight of the service date
    departure_minutes    int          CHECK (departure_minutes BETWEEN 0 AND 2879),
    default_platform_id  uuid,
    kind                 varchar(20)  NOT NULL CHECK (kind IN ('Stop', 'PickUpOnly', 'SetDownOnly', 'Passing')),
    PRIMARY KEY (service_id, sequence),
    UNIQUE (service_id, station_id),
    FOREIGN KEY (default_platform_id, station_id) REFERENCES network.platforms (id, station_id),
    CHECK (arrival_minutes IS NOT NULL OR departure_minutes IS NOT NULL),
    CHECK (arrival_minutes IS NULL OR departure_minutes IS NULL OR arrival_minutes <= departure_minutes)
);
CREATE INDEX ix_service_stops_station ON network.service_stops (station_id, departure_minutes);

-- =====================================================================================
-- JOURNEYS  (live operations: journey state, ingest, fleet, crew, assignments)
-- Identity: the journey id. Vehicles and crews attach through time-ranged assignments.
-- =====================================================================================

CREATE TABLE journeys.vehicles (                                 -- consist (trainset, or locomotive + coaches)
    id         uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    code       varchar(30)  NOT NULL UNIQUE,
    name       varchar(100) NOT NULL,
    kind       varchar(12)  NOT NULL CHECK (kind IN ('Trainset', 'Locomotive')),
    is_active  boolean      NOT NULL DEFAULT true
);

CREATE TABLE journeys.vehicle_unavailability (                   -- REQ-OPS-012
    id            uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    vehicle_id    uuid         NOT NULL REFERENCES journeys.vehicles (id),
    from_utc      timestamptz  NOT NULL,
    to_utc        timestamptz  NOT NULL,
    reason        varchar(300) NOT NULL,
    set_by        varchar(64)  NOT NULL,
    created_at    timestamptz  NOT NULL DEFAULT now(),
    cancelled_at  timestamptz,
    CHECK (to_utc > from_utc)
);
CREATE INDEX ix_vehicle_unavailability_window ON journeys.vehicle_unavailability (vehicle_id, from_utc, to_utc) WHERE cancelled_at IS NULL;

CREATE TABLE journeys.journeys (
    id                         uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    service_id                 uuid,                              -- -> network.services.id ; null for a manual special or relief service
    timetable_version_id       uuid,                              -- -> network.timetable_versions.id ; the version it was generated from
    service_date               date         NOT NULL,
    train_number               varchar(10)  NOT NULL,             -- display label
    route_code                 varchar(10)  NOT NULL,             -- -> network.routes.code
    origin_station_code        varchar(5)   NOT NULL,
    destination_station_code   varchar(5)   NOT NULL,
    creation_origin            varchar(10)  NOT NULL CHECK (creation_origin IN ('Generated', 'Manual')),
    manual_reason              varchar(300),
    scheduled_departure_utc    timestamptz  NOT NULL,
    scheduled_arrival_utc      timestamptz  NOT NULL,
    lifecycle                  varchar(20)  NOT NULL CHECK (lifecycle IN ('Planned','Ready','Running','Interrupted','Completed','TerminatedShort','Cancelled','TerminatedEmergency')),
    running_sub_state          varchar(10)  CHECK (running_sub_state IN ('AtStop', 'InSection')),
    current_stop_sequence      int,                               -- last confirmed stop
    delay_minutes              int,                               -- signed delta; null when hidden (Interrupted, Unknown)
    punctuality_band           varchar(12)  CHECK (punctuality_band IN ('Early', 'OnTime', 'MinorDelay', 'MajorDelay')),
    confidence                 varchar(10)  NOT NULL DEFAULT 'Scheduled' CHECK (confidence IN ('Confirmed', 'Live', 'Scheduled', 'Unknown')),
    last_accepted_update_at    timestamptz,                       -- staleness is computed from this at read time
    interrupted_at             timestamptz,
    ended_at                   timestamptz,
    ended_at_stop_sequence     int,                               -- TerminatedShort
    cancel_reason_code         varchar(30),
    version                    int          NOT NULL DEFAULT 0,   -- optimistic concurrency (REQ-OPS-010)
    created_at_utc                 timestamptz  NOT NULL DEFAULT now(),
    updated_at_utc                 timestamptz,
    CHECK (running_sub_state IS NULL OR lifecycle = 'Running'),
    CHECK (lifecycle <> 'Interrupted' OR delay_minutes IS NULL),
    CHECK (creation_origin <> 'Manual' OR manual_reason IS NOT NULL),
    CHECK (scheduled_arrival_utc > scheduled_departure_utc)
);
CREATE UNIQUE INDEX ux_journeys_service_date ON journeys.journeys (service_id, service_date) WHERE service_id IS NOT NULL;   -- generation is idempotent (REQ-LIVE-008)
CREATE INDEX ix_journeys_date_lifecycle ON journeys.journeys (service_date, lifecycle);
CREATE INDEX ix_journeys_active ON journeys.journeys (scheduled_departure_utc)
    WHERE lifecycle IN ('Planned', 'Ready', 'Running', 'Interrupted');

CREATE TABLE journeys.journey_stops (
    journey_id               uuid         NOT NULL REFERENCES journeys.journeys (id) ON DELETE CASCADE,
    sequence                 int          NOT NULL CHECK (sequence >= 1),
    station_code             varchar(5)   NOT NULL,               -- -> network.stations.code
    kind                     varchar(20)  NOT NULL CHECK (kind IN ('Stop', 'PickUpOnly', 'SetDownOnly', 'Passing')),
    state                    varchar(10)  NOT NULL DEFAULT 'Pending' CHECK (state IN ('Pending','Arrived','Departed','Skipped','Cancelled')),
    scheduled_arrival_utc    timestamptz,
    scheduled_departure_utc  timestamptz,                         -- never changes after generation
    estimated_arrival_utc    timestamptz,
    estimated_departure_utc  timestamptz,
    actual_arrival_utc       timestamptz,
    actual_departure_utc     timestamptz,
    estimate_source          varchar(10)  NOT NULL DEFAULT 'Scheduled' CHECK (estimate_source IN ('Confirmed', 'Live', 'Scheduled')),
    dwell_minutes            int          NOT NULL DEFAULT 0 CHECK (dwell_minutes >= 0),
    platform_code            varchar(3),
    platform_state           varchar(8)   NOT NULL DEFAULT 'TBA' CHECK (platform_state IN ('TBA', 'Assigned', 'Changed')),
    platform_original_code   varchar(3),                          -- the first assignment, for "changed from"
    platform_changed_at      timestamptz,
    platform_changed_by      varchar(64),
    PRIMARY KEY (journey_id, sequence)
);
CREATE INDEX ix_journey_stops_board ON journeys.journey_stops (station_code, scheduled_departure_utc) WHERE kind <> 'Passing';
CREATE INDEX ix_journey_stops_arrivals ON journeys.journey_stops (station_code, scheduled_arrival_utc) WHERE kind <> 'Passing';

CREATE TABLE journeys.journey_events (                           -- event history; losing values are kept (REQ-LIVE-006)
    id                        bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    journey_id                uuid         NOT NULL REFERENCES journeys.journeys (id),
    event_type                varchar(30)  NOT NULL CHECK (event_type IN ('Arrived','Departed','Passed','Skipped','Position','DelayReason','Cancelled','TerminatedShort','Reinstated','PlatformChanged','Corrected','StateChanged')),
    stop_sequence             int,
    source_kind               varchar(10)  NOT NULL CHECK (source_kind IN ('Feed', 'Driver', 'Controller', 'System')),
    source_id                 uuid,                               -- -> identity.feed_sources.id
    source_rank               smallint,                           -- the source's precedence rank when the message arrived (SPEC-005), so the history stands on its own
    actor_subject             varchar(64),
    duty_id                   uuid,                               -- driver events are scoped to a duty (REQ-DRV-009)
    occurred_at_source        timestamptz,                        -- device or feed time; never trusted for ordering
    received_at               timestamptz  NOT NULL DEFAULT now(),   -- server time is the clock authority
    device_sequence           bigint,
    idempotency_key           varchar(80),
    payload                   jsonb        NOT NULL DEFAULT '{}',
    disposition               varchar(20)  NOT NULL CHECK (disposition IN ('Applied','Superseded','Rejected','HeldForCorrection','Duplicate','Orphaned','OutsideDutyWindow')),
    disposition_reason        varchar(300),
    superseded_by_event_id    bigint       REFERENCES journeys.journey_events (id)
);
CREATE UNIQUE INDEX ux_events_source_sequence ON journeys.journey_events (source_id, device_sequence) WHERE source_id IS NOT NULL AND device_sequence IS NOT NULL;
CREATE UNIQUE INDEX ux_events_idempotency ON journeys.journey_events (idempotency_key) WHERE idempotency_key IS NOT NULL;   -- replay is a no-op (REQ-DRV-006)
CREATE INDEX ix_events_journey ON journeys.journey_events (journey_id, received_at);
CREATE INDEX ix_events_review ON journeys.journey_events (disposition) WHERE disposition IN ('HeldForCorrection', 'Orphaned', 'OutsideDutyWindow');
CREATE TRIGGER trg_journey_events_no_delete BEFORE DELETE ON journeys.journey_events FOR EACH ROW EXECUTE FUNCTION common.forbid_delete();

CREATE TABLE journeys.journey_delay_reasons (                    -- driver reason kept; controller may reclassify, original retained
    id                 uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    journey_id         uuid         NOT NULL REFERENCES journeys.journeys (id),
    reason_code        varchar(30)  NOT NULL,                     -- -> operations.reason_codes.code
    recorded_by        varchar(64)  NOT NULL,
    recorded_by_role   varchar(12)  NOT NULL CHECK (recorded_by_role IN ('Driver', 'Controller')),
    duty_id            uuid,
    recorded_at        timestamptz  NOT NULL DEFAULT now(),
    reclassifies_id    uuid         REFERENCES journeys.journey_delay_reasons (id)
);
CREATE INDEX ix_delay_reasons_journey ON journeys.journey_delay_reasons (journey_id, recorded_at);

CREATE TABLE journeys.telemetry_readings (                       -- raw feed data, 7 days (REQ-DATA-003); legal hold overrides purge
    id                         bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    source_id                  uuid         NOT NULL,            -- -> identity.feed_sources.id
    vehicle_code               varchar(30)  NOT NULL,
    recorded_at                timestamptz  NOT NULL,
    received_at                timestamptz  NOT NULL DEFAULT now(),
    sequence                   bigint       NOT NULL,
    section_from_station_code  varchar(5),
    section_to_station_code    varchar(5),
    progress                   numeric(5,4) CHECK (progress BETWEEN 0 AND 1),
    speed_kmh                  numeric(5,1) CHECK (speed_kmh >= 0),
    resolved_journey_id        uuid,                              -- null = ghost movement, never shown publicly (REQ-LIVE-015)
    accepted                   boolean      NOT NULL,
    reject_reason              varchar(100),
    legal_hold                 boolean      NOT NULL DEFAULT false,
    UNIQUE (source_id, sequence)
);
CREATE INDEX ix_telemetry_vehicle ON journeys.telemetry_readings (vehicle_code, recorded_at DESC);
CREATE INDEX ix_telemetry_purge ON journeys.telemetry_readings (received_at) WHERE legal_hold = false;

CREATE TABLE journeys.vehicle_assignments (                      -- REQ-LIVE-010: time-ranged
    id           uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    journey_id   uuid         NOT NULL REFERENCES journeys.journeys (id),
    vehicle_id   uuid         NOT NULL REFERENCES journeys.vehicles (id),
    valid_from   timestamptz  NOT NULL,
    valid_to     timestamptz,
    assigned_by  varchar(64)  NOT NULL,
    reason       varchar(300),
    created_at   timestamptz  NOT NULL DEFAULT now(),
    CHECK (valid_to IS NULL OR valid_to > valid_from),
    EXCLUDE USING gist (journey_id WITH =, tstzrange(valid_from, valid_to) WITH &&),   -- one vehicle per journey at a time
    EXCLUDE USING gist (vehicle_id WITH =, tstzrange(valid_from, valid_to) WITH &&)    -- a vehicle is not double-booked
);

CREATE TABLE journeys.crew_duties (                              -- REQ-DRV-001..009; state model section 6
    id                    uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    driver_subject        varchar(64)  NOT NULL,                  -- -> identity.staff_profiles.subject
    service_date          date         NOT NULL,
    rostered_from         timestamptz  NOT NULL,
    rostered_to           timestamptz  NOT NULL,
    state                 varchar(10)  NOT NULL DEFAULT 'Rostered' CHECK (state IN ('Rostered','SignedOn','OnJourney','SignedOff','Orphaned')),
    signed_on_at          timestamptz,
    signed_off_at         timestamptz,
    handover_from_duty_id uuid         REFERENCES journeys.crew_duties (id),
    review_state          varchar(11)  NOT NULL DEFAULT 'None' CHECK (review_state IN ('None', 'NeedsReview', 'Reviewed')),
    created_at            timestamptz  NOT NULL DEFAULT now(),
    CHECK (rostered_to > rostered_from),
    EXCLUDE USING gist (driver_subject WITH =, tstzrange(rostered_from, rostered_to) WITH &&)   -- driver double-booking is rejected (UC-OPS-06 F1)
);
CREATE INDEX ix_crew_duties_driver ON journeys.crew_duties (driver_subject, service_date);

CREATE TABLE journeys.driver_assignments (
    id           uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    journey_id   uuid         NOT NULL REFERENCES journeys.journeys (id),
    duty_id      uuid         NOT NULL REFERENCES journeys.crew_duties (id),
    valid_from   timestamptz  NOT NULL,
    valid_to     timestamptz,
    assigned_by  varchar(64)  NOT NULL,
    created_at   timestamptz  NOT NULL DEFAULT now(),
    CHECK (valid_to IS NULL OR valid_to > valid_from),
    EXCLUDE USING gist (journey_id WITH =, tstzrange(valid_from, valid_to) WITH &&)   -- the outgoing duty closes before the incoming opens (REQ-DRV-009)
);
CREATE INDEX ix_driver_assignments_duty ON journeys.driver_assignments (duty_id);

CREATE TABLE journeys.duty_location_samples (                    -- only while signed on, 7 days, never public (REQ-DRV-010)
    id           bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    duty_id      uuid          NOT NULL REFERENCES journeys.crew_duties (id),
    recorded_at  timestamptz   NOT NULL,
    latitude     numeric(9,6)  NOT NULL CHECK (latitude BETWEEN -90 AND 90),
    longitude    numeric(9,6)  NOT NULL CHECK (longitude BETWEEN -180 AND 180),
    accuracy_m   numeric(6,1)
);
CREATE INDEX ix_duty_location_purge ON journeys.duty_location_samples (recorded_at);

CREATE TABLE journeys.punctuality_daily (                        -- aggregate kept 13 months (REQ-OPS-014, REQ-DATA-003)
    service_date           date          NOT NULL,
    route_code             varchar(10)   NOT NULL,
    train_number           varchar(10)   NOT NULL,
    journeys_count         int           NOT NULL,
    on_time_count          int           NOT NULL,
    minor_delay_count      int           NOT NULL,
    major_delay_count      int           NOT NULL,
    cancelled_count        int           NOT NULL,
    terminated_short_count int           NOT NULL,
    average_delay_minutes  numeric(6,2),
    PRIMARY KEY (service_date, route_code, train_number)
);

-- =====================================================================================
-- OPERATIONS  (controller actions, disruptions, incidents, driver messages)
-- =====================================================================================

CREATE TABLE operations.reason_codes (
    category    varchar(20)  NOT NULL CHECK (category IN ('Delay','Cancel','TerminateShort','PlatformChange','Skip','DataCorrection','Reassign','Assistance')),
    code        varchar(30)  NOT NULL,
    label_en    varchar(100) NOT NULL,
    label_si    varchar(100),
    is_active   boolean      NOT NULL DEFAULT true,
    sort_order  int          NOT NULL DEFAULT 0,
    PRIMARY KEY (category, code)
);

CREATE TABLE operations.message_templates (                      -- approved text for severe incidents (REQ-SEC-005, REQ-OPS-008)
    code        varchar(30)  PRIMARY KEY,
    severity    varchar(10)  NOT NULL CHECK (severity IN ('Info', 'Minor', 'Major', 'Critical')),
    text_en     varchar(500) NOT NULL,
    text_si     varchar(500),
    is_active   boolean      NOT NULL DEFAULT true
);

CREATE TABLE operations.disruptions (                            -- state model section 4
    id                        uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    state                     varchar(10)  NOT NULL CHECK (state IN ('Draft','Scheduled','Published','Updated','Resolved','Retracted','Expired')),
    severity                  varchar(10)  NOT NULL CHECK (severity IN ('Info', 'Minor', 'Major', 'Critical')),
    scope_type                varchar(10)  NOT NULL CHECK (scope_type IN ('Network','Route','Section','Station','Service','Journey')),
    scope_route_code          varchar(10),
    scope_from_station_code   varchar(5),
    scope_to_station_code     varchar(5),
    scope_station_code        varchar(5),
    scope_service_id          uuid,
    scope_journey_id          uuid,
    message                   varchar(500) NOT NULL,              -- plain text, escaped on render (REQ-SEC-005)
    template_code             varchar(30)  REFERENCES operations.message_templates (code),
    is_planned                boolean      NOT NULL DEFAULT false, -- engineering works (REQ-OPS-016)
    starts_at                 timestamptz  NOT NULL,
    ends_at                   timestamptz,
    review_by_at              timestamptz  NOT NULL,              -- mandatory (REQ-OPS-006)
    author_subject            varchar(64)  NOT NULL,
    approver_subject          varchar(64),
    approved_at               timestamptz,
    published_at              timestamptz,
    resolved_at               timestamptz,
    retracted_at              timestamptz,
    retraction_reason         varchar(300),
    push_sent                 boolean      NOT NULL DEFAULT false,
    correction_message        varchar(500),
    version                   int          NOT NULL DEFAULT 0,
    created_at_utc                timestamptz  NOT NULL DEFAULT now(),
    updated_at_utc                timestamptz,
    CHECK (ends_at IS NULL OR ends_at > starts_at),
    CHECK ((scope_type = 'Network'  AND scope_route_code IS NULL AND scope_station_code IS NULL AND scope_service_id IS NULL AND scope_journey_id IS NULL)
        OR (scope_type = 'Route'    AND scope_route_code IS NOT NULL)
        OR (scope_type = 'Section'  AND scope_route_code IS NOT NULL AND scope_from_station_code IS NOT NULL AND scope_to_station_code IS NOT NULL)
        OR (scope_type = 'Station'  AND scope_station_code IS NOT NULL)
        OR (scope_type = 'Service'  AND scope_service_id IS NOT NULL)
        OR (scope_type = 'Journey'  AND scope_journey_id IS NOT NULL)),
    -- Network-wide publication needs a second approver (REQ-OPS-005)
    CHECK (NOT (scope_type = 'Network' AND state IN ('Published','Updated','Resolved','Expired')
                AND (approver_subject IS NULL OR approver_subject = author_subject)))
);
CREATE INDEX ix_disruptions_state ON operations.disruptions (state, starts_at);
CREATE INDEX ix_disruptions_review ON operations.disruptions (review_by_at) WHERE state IN ('Published', 'Updated');

CREATE TABLE operations.disruption_revisions (                   -- "Updated: history kept"
    id             uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    disruption_id  uuid         NOT NULL REFERENCES operations.disruptions (id),
    revision_no    int          NOT NULL,
    severity       varchar(10)  NOT NULL,
    message        varchar(500) NOT NULL,
    changed_by     varchar(64)  NOT NULL,
    changed_at     timestamptz  NOT NULL DEFAULT now(),
    UNIQUE (disruption_id, revision_no)
);

CREATE TABLE operations.disruption_affected_journeys (           -- scope resolved to journeys and stops (REQ-LIVE-016)
    disruption_id      uuid  NOT NULL REFERENCES operations.disruptions (id) ON DELETE CASCADE,
    journey_id         uuid  NOT NULL,                            -- -> journeys.journeys.id
    from_stop_sequence int,
    to_stop_sequence   int,
    PRIMARY KEY (disruption_id, journey_id)
);

CREATE TABLE operations.incidents (                              -- REQ-OPS-007
    id                   uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    severity             varchar(10)  NOT NULL CHECK (severity IN ('Minor', 'Major', 'Critical')),
    state                varchar(10)  NOT NULL DEFAULT 'Open' CHECK (state IN ('Open', 'Resolved')),
    summary              varchar(500) NOT NULL,
    owner_subject        varchar(64)  NOT NULL,
    declared_by          varchar(64)  NOT NULL,
    declared_at          timestamptz  NOT NULL DEFAULT now(),
    resolved_at          timestamptz,
    related_journey_id   uuid,                                    -- -> journeys.journeys.id
    confirmation_recorded boolean     NOT NULL DEFAULT false,
    CHECK (severity <> 'Critical' OR confirmation_recorded),      -- Critical needs the confirmation step
    CHECK ((state = 'Resolved') = (resolved_at IS NOT NULL))
);

CREATE TABLE operations.incident_timeline_entries (
    id           uuid          PRIMARY KEY DEFAULT gen_random_uuid(),
    incident_id  uuid          NOT NULL REFERENCES operations.incidents (id),
    at           timestamptz   NOT NULL DEFAULT now(),
    author       varchar(64)   NOT NULL,
    kind         varchar(15)   NOT NULL CHECK (kind IN ('Declared','Note','OwnerChanged','SeverityChanged','Resolved')),
    text         varchar(1000)
);

CREATE TABLE operations.legal_holds (                            -- overrides retention (REQ-DATA-003, REQ-OPS-008)
    id           uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    incident_id  uuid         REFERENCES operations.incidents (id),
    journey_id   uuid         NOT NULL,                           -- -> journeys.journeys.id
    placed_by    varchar(64)  NOT NULL,
    placed_at    timestamptz  NOT NULL DEFAULT now(),
    reason       varchar(300) NOT NULL,
    released_by  varchar(64),
    released_at  timestamptz
);
CREATE INDEX ix_legal_holds_active ON operations.legal_holds (journey_id) WHERE released_at IS NULL;

CREATE TABLE operations.journey_actions (                        -- every controller action with its reason (REQ-OPS-004)
    id                uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    journey_id        uuid         NOT NULL,                      -- -> journeys.journeys.id
    action_type       varchar(20)  NOT NULL CHECK (action_type IN ('Cancel','TerminateShort','TerminateEmergency','Reinstate','PlatformChange','ReassignVehicle','ReassignDriver','CorrectData','SkipStop')),
    stop_sequence     int,
    reason_code       varchar(30)  NOT NULL,
    reason_text       varchar(500),
    performed_by      varchar(64)  NOT NULL,
    performed_role    varchar(30)  NOT NULL,
    performed_at      timestamptz  NOT NULL DEFAULT now(),
    journey_version   int,
    details           jsonb        NOT NULL DEFAULT '{}',
    correlation_id    varchar(64)
);
CREATE INDEX ix_journey_actions_journey ON operations.journey_actions (journey_id, performed_at);

CREATE TABLE operations.driver_messages (                        -- REQ-OPS-013, REQ-DRV-007
    id                  uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    driver_subject      varchar(64)  NOT NULL,
    duty_id             uuid,                                     -- -> journeys.crew_duties.id
    journey_id          uuid,
    text                varchar(500) NOT NULL,                    -- plain text
    sent_by             varchar(64)  NOT NULL,
    sent_at             timestamptz  NOT NULL DEFAULT now(),
    valid_until         timestamptz  NOT NULL,
    state               varchar(12)  NOT NULL DEFAULT 'Sent' CHECK (state IN ('Sent', 'Delivered', 'Acknowledged')),
    delivered_at        timestamptz,
    acknowledged_at     timestamptz,
    expiry_alerted_at   timestamptz,
    CHECK (valid_until > sent_at)
);
CREATE INDEX ix_driver_messages_driver ON operations.driver_messages (driver_subject, sent_at DESC);
CREATE INDEX ix_driver_messages_open ON operations.driver_messages (valid_until) WHERE state <> 'Acknowledged';

CREATE TABLE operations.assistance_requests (                    -- UC-DRV-06
    id                       uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    journey_id               uuid         NOT NULL,
    duty_id                  uuid         NOT NULL,
    driver_subject           varchar(64)  NOT NULL,
    request_type             varchar(15)  NOT NULL CHECK (request_type IN ('Medical','Overcrowding','UnsafePassenger','Other')),
    status                   varchar(12)  NOT NULL DEFAULT 'Open' CHECK (status IN ('Open', 'Acknowledged', 'Closed')),
    urgent                   boolean      NOT NULL DEFAULT true,
    queued_offline           boolean      NOT NULL DEFAULT false,
    last_known_stop_sequence int,
    idempotency_key          varchar(80)  NOT NULL UNIQUE,
    requested_at             timestamptz  NOT NULL DEFAULT now(),
    acknowledged_by          varchar(64),
    acknowledged_at          timestamptz,
    closed_at                timestamptz
);
CREATE INDEX ix_assistance_open ON operations.assistance_requests (status, requested_at) WHERE status <> 'Closed';

-- =====================================================================================
-- NOTIFICATIONS  (anonymous device subscriptions, follows, inbox)
-- =====================================================================================

CREATE TABLE notifications.device_subscriptions (                -- REQ-PAX-007, REQ-PAX-012, REQ-PAX-015
    id                    uuid         PRIMARY KEY,               -- generated on the device; not a user id
    platform              varchar(7)   NOT NULL CHECK (platform IN ('Android', 'iOS')),
    push_token            varchar(512),                           -- never logged
    push_token_invalid_at timestamptz,
    language              varchar(10)  NOT NULL DEFAULT 'en',
    created_at            timestamptz  NOT NULL DEFAULT now(),
    last_seen_at          timestamptz  NOT NULL DEFAULT now()     -- expired after 90 days of inactivity
);
CREATE INDEX ix_device_subscriptions_seen ON notifications.device_subscriptions (last_seen_at);

CREATE TABLE notifications.journey_follows (
    device_id                 uuid         NOT NULL REFERENCES notifications.device_subscriptions (id) ON DELETE CASCADE,
    journey_id                uuid         NOT NULL,              -- -> journeys.journeys.id
    source                    varchar(6)   NOT NULL DEFAULT 'Manual' CHECK (source IN ('Manual', 'Ticket')),
    followed_at               timestamptz  NOT NULL DEFAULT now(),
    last_pushed_at            timestamptz,                        -- throttle: one routine push per 5 minutes (REQ-PAX-007)
    last_pushed_delay_minutes int,
    PRIMARY KEY (device_id, journey_id)
);
CREATE INDEX ix_follows_journey ON notifications.journey_follows (journey_id);

CREATE TABLE notifications.notifications (                       -- in-app inbox; every push is stored here (REQ-PAX-015)
    id                       uuid         PRIMARY KEY DEFAULT gen_random_uuid(),
    device_id                uuid         NOT NULL REFERENCES notifications.device_subscriptions (id) ON DELETE CASCADE,
    event_type               varchar(40)  NOT NULL,
    journey_id               uuid,
    disruption_id            uuid,
    title                    varchar(120) NOT NULL,
    body                     varchar(300) NOT NULL,               -- states what changed, old to new (REQ-PAX-008)
    old_value                varchar(100),
    new_value                varchar(100),
    is_correction            boolean      NOT NULL DEFAULT false,
    corrects_notification_id uuid         REFERENCES notifications.notifications (id),
    delivery_status          varchar(9)   NOT NULL DEFAULT 'Pending' CHECK (delivery_status IN ('Pending','Sent','Failed','NotPushed')),
    attempts                 smallint     NOT NULL DEFAULT 0 CHECK (attempts <= 2),   -- one retry (REQ-PAX-015)
    last_attempt_at          timestamptz,
    created_at               timestamptz  NOT NULL DEFAULT now(),
    read_at                  timestamptz
);
CREATE INDEX ix_notifications_device ON notifications.notifications (device_id, created_at DESC);

-- =====================================================================================
-- TICKETING  (accounts' tickets, payment port, refunds, scans)   mvp-scope: Yes
-- =====================================================================================

CREATE TABLE ticketing.fare_categories (
    code        varchar(10)  PRIMARY KEY CHECK (code IN ('Adult', 'Child', 'Senior')),
    label       varchar(50)  NOT NULL,
    multiplier  numeric(4,2) NOT NULL CHECK (multiplier > 0)
);

CREATE TABLE ticketing.fare_tariffs (                            -- seeded stage-based table (REQ-TKT-003, ASM-039)
    id               uuid          PRIMARY KEY DEFAULT gen_random_uuid(),
    code             varchar(20)   NOT NULL UNIQUE,
    currency         char(3)       NOT NULL DEFAULT 'LKR',
    base_amount      numeric(10,2) NOT NULL CHECK (base_amount >= 0),
    rate_per_stage   numeric(10,2) NOT NULL CHECK (rate_per_stage >= 0),
    excess_penalty   numeric(10,2) NOT NULL CHECK (excess_penalty >= 0),   -- flat part of the excess fare (REQ-GAT-005)
    effective_from   date          NOT NULL,
    effective_to     date,
    CHECK (effective_to IS NULL OR effective_to >= effective_from)
);

CREATE TABLE ticketing.tickets (
    id                         uuid          PRIMARY KEY DEFAULT gen_random_uuid(),
    account_subject            varchar(64),                       -- -> identity.passenger_accounts.subject ; null after anonymisation (REQ-ACC-003)
    journey_id                 uuid          NOT NULL,            -- -> journeys.journeys.id (REQ-TKT-001)
    service_date               date          NOT NULL,
    origin_station_code        varchar(5)    NOT NULL,
    origin_stop_sequence       int           NOT NULL,
    destination_station_code   varchar(5)    NOT NULL,
    destination_stop_sequence  int           NOT NULL,
    fare_category              varchar(10)   NOT NULL REFERENCES ticketing.fare_categories (code),
    tariff_id                  uuid          NOT NULL REFERENCES ticketing.fare_tariffs (id),
    price_amount               numeric(10,2) NOT NULL CHECK (price_amount >= 0),   -- computed server-side only
    currency                   char(3)       NOT NULL DEFAULT 'LKR',
    needs_assistance           boolean       NOT NULL DEFAULT false,              -- separate from the fare category (REQ-TKT-004)
    state                      varchar(14)   NOT NULL CHECK (state IN ('PendingPayment','Valid','Boarded','Completed','Affected','Refunded','Expired')),
    qr_key_id                  smallint      NOT NULL DEFAULT 1,
    qr_nonce                   varchar(40)   NOT NULL UNIQUE,                     -- the QR carries ids only, no personal data (REQ-TKT-012)
    expires_at                 timestamptz,                                       -- PendingPayment: created + 15 minutes (REQ-TKT-007)
    flagged_no_exit_scan       boolean       NOT NULL DEFAULT false,
    affected_at                timestamptz,
    version                    int           NOT NULL DEFAULT 0,
    created_at_utc                 timestamptz   NOT NULL DEFAULT now(),
    updated_at_utc                 timestamptz,
    CHECK (origin_stop_sequence < destination_stop_sequence),
    CHECK (state <> 'PendingPayment' OR expires_at IS NOT NULL)
);
CREATE INDEX ix_tickets_account ON ticketing.tickets (account_subject, created_at DESC);
CREATE INDEX ix_tickets_journey_state ON ticketing.tickets (journey_id, state);
CREATE INDEX ix_tickets_pending_expiry ON ticketing.tickets (expires_at) WHERE state = 'PendingPayment';

CREATE TABLE ticketing.ticket_status_history (                   -- append-only (REQ-TKT-005)
    id              bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    ticket_id       uuid         NOT NULL REFERENCES ticketing.tickets (id),
    from_state      varchar(14),
    to_state        varchar(14)  NOT NULL,
    trigger         varchar(40)  NOT NULL,
    actor           varchar(64)  NOT NULL,
    reason          varchar(300),
    occurred_at     timestamptz  NOT NULL DEFAULT now(),
    correlation_id  varchar(64)
);
CREATE INDEX ix_ticket_history_ticket ON ticketing.ticket_status_history (ticket_id, occurred_at);

CREATE TABLE ticketing.payment_attempts (
    id                  uuid          PRIMARY KEY DEFAULT gen_random_uuid(),
    ticket_id           uuid          NOT NULL REFERENCES ticketing.tickets (id),
    provider            varchar(10)   NOT NULL CHECK (provider IN ('Fake', 'PayHere')),
    provider_reference  varchar(100),
    idempotency_key     varchar(80)   NOT NULL UNIQUE,
    amount              numeric(10,2) NOT NULL CHECK (amount > 0),
    currency            char(3)       NOT NULL DEFAULT 'LKR',
    status              varchar(10)   NOT NULL CHECK (status IN ('Created','Pending','Succeeded','Failed','Refunded')),
    failure_reason      varchar(200),
    created_at          timestamptz   NOT NULL DEFAULT now(),
    confirmed_at        timestamptz                                 -- card data is never stored (REQ-PAY-001)
);
CREATE UNIQUE INDEX ux_payment_provider_reference ON ticketing.payment_attempts (provider, provider_reference) WHERE provider_reference IS NOT NULL;   -- duplicate callback, no second ticket (REQ-PAY-002)

CREATE TABLE ticketing.payment_callbacks (                       -- every callback is logged and counted (REQ-PAY-003)
    id                  bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    provider            varchar(10)  NOT NULL,
    provider_reference  varchar(100),
    received_at         timestamptz  NOT NULL DEFAULT now(),
    signature_valid     boolean      NOT NULL,
    timestamp_in_window boolean      NOT NULL,                      -- 5-minute replay window
    outcome             varchar(9)   NOT NULL CHECK (outcome IN ('Accepted', 'Rejected', 'Duplicate')),
    payload_hash        varchar(64)
);

CREATE TABLE ticketing.refunds (                                 -- REQ-REF-001..005
    id                         uuid          PRIMARY KEY DEFAULT gen_random_uuid(),
    ticket_id                  uuid          NOT NULL REFERENCES ticketing.tickets (id),
    payment_attempt_id         uuid          NOT NULL REFERENCES ticketing.payment_attempts (id),
    amount                     numeric(10,2) NOT NULL CHECK (amount > 0),
    currency                   char(3)       NOT NULL DEFAULT 'LKR',
    reason                     varchar(18)   NOT NULL CHECK (reason IN ('JourneyCancelled','NoTicketIssued','PassengerRequested')),
    state                      varchar(10)   NOT NULL CHECK (state IN ('Requested','Processing','Refunded','Failed')),
    attempts                   smallint      NOT NULL DEFAULT 0 CHECK (attempts >= 0),   -- 3 automatic attempts with backoff (REQ-REF-004)
    next_attempt_at            timestamptz,
    last_error                 varchar(300),
    provider_refund_reference  varchar(100),
    requested_at               timestamptz   NOT NULL DEFAULT now(),
    completed_at               timestamptz
);
CREATE UNIQUE INDEX ux_refund_one_active ON ticketing.refunds (ticket_id) WHERE state IN ('Requested', 'Processing', 'Refunded');   -- no double refund (REQ-REF-003)
CREATE INDEX ix_refund_retry ON ticketing.refunds (next_attempt_at) WHERE state IN ('Requested', 'Processing', 'Failed');

CREATE TABLE ticketing.refund_status_history (
    id           bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    refund_id    uuid         NOT NULL REFERENCES ticketing.refunds (id),
    from_state   varchar(10),
    to_state     varchar(10)  NOT NULL,
    detail       varchar(300),
    occurred_at  timestamptz  NOT NULL DEFAULT now()
);

CREATE TABLE ticketing.scans (                                   -- append-only scan log (REQ-GAT-009, REQ-INS-002)
    id                   uuid          PRIMARY KEY DEFAULT gen_random_uuid(),
    client_scan_id       varchar(64)   NOT NULL,
    scan_type            varchar(7)    NOT NULL CHECK (scan_type IN ('Board', 'Exit', 'Inspect')),
    device_id            uuid,                                    -- -> identity.devices.id (gate); fixes the station
    inspector_subject    varchar(64),                             -- -> identity.staff_profiles.subject
    ticket_id            uuid          REFERENCES ticketing.tickets (id),   -- null when the QR is tampered or unknown
    journey_id           uuid,
    station_code         varchar(5),
    stop_sequence        int,
    stop_was_manual      boolean       NOT NULL DEFAULT false,    -- inspector changed the default stop (REQ-INS-006)
    result_code          varchar(40)   NOT NULL CHECK (result_code IN ('VALID','NOT_VALID_STATUS','WRONG_JOURNEY','WRONG_DATE','WRONG_STATION','OUTSIDE_SEGMENT_PAST_DESTINATION','OUTSIDE_SEGMENT_BEFORE_ORIGIN','TAMPERED','UNABLE_TO_VERIFY','ALREADY_BOARDED')),
    flag                 varchar(20)   CHECK (flag IN ('EarlyExit', 'NoExitScan', 'Reused')),
    excess_fare_amount   numeric(10,2) CHECK (excess_fare_amount >= 0),
    device_time          timestamptz,                             -- recorded, never trusted for decisions (REQ-GAT-007)
    scanned_at           timestamptz   NOT NULL DEFAULT now(),
    CHECK (device_id IS NOT NULL OR inspector_subject IS NOT NULL)
);
CREATE UNIQUE INDEX ux_scans_device_client ON ticketing.scans (device_id, client_scan_id) WHERE device_id IS NOT NULL;               -- a retried delivery is idempotent (REQ-GAT-004)
CREATE UNIQUE INDEX ux_scans_inspector_client ON ticketing.scans (inspector_subject, client_scan_id) WHERE inspector_subject IS NOT NULL;
CREATE INDEX ix_scans_station ON ticketing.scans (station_code, scanned_at DESC);
CREATE INDEX ix_scans_ticket ON ticketing.scans (ticket_id);

CREATE TABLE ticketing.excess_fares (                            -- list for the Station Master (REQ-OPS-018)
    id             uuid          PRIMARY KEY DEFAULT gen_random_uuid(),
    scan_id        uuid          NOT NULL UNIQUE REFERENCES ticketing.scans (id),
    ticket_id      uuid          NOT NULL REFERENCES ticketing.tickets (id),
    station_code   varchar(5)    NOT NULL,
    amount         numeric(10,2) NOT NULL CHECK (amount > 0),
    state          varchar(9)    NOT NULL DEFAULT 'Due' CHECK (state IN ('Due', 'Collected')),
    collected_by   varchar(64),
    collected_at   timestamptz,
    created_at     timestamptz   NOT NULL DEFAULT now(),
    CHECK ((state = 'Collected') = (collected_at IS NOT NULL))
);
CREATE INDEX ix_excess_fares_station ON ticketing.excess_fares (station_code, state);

-- =====================================================================================
-- CROSS-CUTTING  audit_entries in every module that performs high-consequence actions
-- (REQ-OPS-011). Written in the same transaction as the change. Append-only.
-- =====================================================================================

DO $$
DECLARE s text;
BEGIN
    FOREACH s IN ARRAY ARRAY['identity', 'network', 'journeys', 'operations', 'ticketing'] LOOP
        EXECUTE format($f$
            CREATE TABLE %1$I.audit_entries (
                id              bigint GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                occurred_at     timestamptz  NOT NULL DEFAULT now(),
                actor_subject   varchar(64)  NOT NULL,
                actor_role      varchar(30),
                action          varchar(80)  NOT NULL,
                target_type     varchar(40)  NOT NULL,
                target_id       varchar(64),
                reason          varchar(500),
                before_state    jsonb,
                after_state     jsonb,
                correlation_id  varchar(64)
            )$f$, s);
        EXECUTE format('CREATE INDEX ix_audit_%1$s_time ON %1$I.audit_entries (occurred_at DESC)', s);
        EXECUTE format('CREATE INDEX ix_audit_%1$s_target ON %1$I.audit_entries (target_type, target_id)', s);
        EXECUTE format('CREATE TRIGGER trg_audit_%1$s_append_only BEFORE UPDATE OR DELETE ON %1$I.audit_entries FOR EACH ROW EXECUTE FUNCTION common.forbid_update_delete()', s);
    END LOOP;
END $$;

-- Append-only business tables
CREATE TRIGGER trg_ticket_history_append_only  BEFORE UPDATE OR DELETE ON ticketing.ticket_status_history FOR EACH ROW EXECUTE FUNCTION common.forbid_update_delete();
CREATE TRIGGER trg_refund_history_append_only  BEFORE UPDATE OR DELETE ON ticketing.refund_status_history FOR EACH ROW EXECUTE FUNCTION common.forbid_update_delete();
CREATE TRIGGER trg_scans_append_only           BEFORE UPDATE OR DELETE ON ticketing.scans FOR EACH ROW EXECUTE FUNCTION common.forbid_update_delete();
CREATE TRIGGER trg_payment_callbacks_append    BEFORE UPDATE OR DELETE ON ticketing.payment_callbacks FOR EACH ROW EXECUTE FUNCTION common.forbid_update_delete();
CREATE TRIGGER trg_journey_actions_append_only BEFORE UPDATE OR DELETE ON operations.journey_actions FOR EACH ROW EXECUTE FUNCTION common.forbid_update_delete();
CREATE TRIGGER trg_disruption_revisions_append BEFORE UPDATE OR DELETE ON operations.disruption_revisions FOR EACH ROW EXECUTE FUNCTION common.forbid_update_delete();
CREATE TRIGGER trg_delay_reasons_append_only   BEFORE UPDATE OR DELETE ON journeys.journey_delay_reasons FOR EACH ROW EXECUTE FUNCTION common.forbid_update_delete();
CREATE TRIGGER trg_incident_timeline_append    BEFORE UPDATE OR DELETE ON operations.incident_timeline_entries FOR EACH ROW EXECUTE FUNCTION common.forbid_update_delete();

-- =====================================================================================
-- OUTSIDE THIS DATABASE
--   * Keycloak keeps its own database (users, credentials, role membership, sessions).
--   * RailBite (catering, Node.js) keeps its own database: catering_stations(station_code, name,
--     phone, is_active), menu_categories(id, name, sort_order), menu_items(id, station_code,
--     category_id, name, price, currency, is_available). SmartRail never reads it (REQ-INT-001).
-- =====================================================================================
