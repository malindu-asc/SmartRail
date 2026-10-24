using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SmartRail.Modules.Journeys.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Time-range exclusion constraints below need btree_gist (a trusted extension on PostgreSQL 13+).
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");

            migrationBuilder.EnsureSchema(
                name: "journeys");

            migrationBuilder.CreateTable(
                name: "audit_entries",
                schema: "journeys",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actor_subject = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    actor_role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    action = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    target_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    target_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    before_state = table.Column<string>(type: "jsonb", nullable: true),
                    after_state = table.Column<string>(type: "jsonb", nullable: true),
                    correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "crew_duties",
                schema: "journeys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    driver_subject = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    service_date = table.Column<DateOnly>(type: "date", nullable: false),
                    rostered_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    rostered_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    state = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    signed_on_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    signed_off_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    handover_from_duty_id = table.Column<Guid>(type: "uuid", nullable: true),
                    review_state = table.Column<string>(type: "character varying(11)", maxLength: 11, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_crew_duties", x => x.id);
                    table.CheckConstraint("ck_crew_duties_review", "review_state in ('None','NeedsReview','Reviewed')");
                    table.CheckConstraint("ck_crew_duties_state", "state in ('Rostered','SignedOn','OnJourney','SignedOff','Orphaned')");
                    table.CheckConstraint("ck_crew_duties_window", "rostered_to > rostered_from");
                    table.ForeignKey(
                        name: "FK_crew_duties_crew_duties_handover_from_duty_id",
                        column: x => x.handover_from_duty_id,
                        principalSchema: "journeys",
                        principalTable: "crew_duties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "journeys",
                schema: "journeys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: true),
                    timetable_version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    service_date = table.Column<DateOnly>(type: "date", nullable: false),
                    train_number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    route_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    origin_station_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    destination_station_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    creation_origin = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    manual_reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    scheduled_departure_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    scheduled_arrival_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    lifecycle = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    running_sub_state = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    current_stop_sequence = table.Column<int>(type: "integer", nullable: true),
                    delay_minutes = table.Column<int>(type: "integer", nullable: true),
                    punctuality_band = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    confidence = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    last_accepted_update_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    interrupted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ended_at_stop_sequence = table.Column<int>(type: "integer", nullable: true),
                    cancel_reason_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    version = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journeys", x => x.id);
                    table.CheckConstraint("ck_journeys_interrupted_delay", "lifecycle <> 'Interrupted' or delay_minutes is null");
                    table.CheckConstraint("ck_journeys_lifecycle", "lifecycle in ('Planned','Ready','Running','Interrupted','Completed','TerminatedShort','Cancelled','TerminatedEmergency')");
                    table.CheckConstraint("ck_journeys_manual_reason", "creation_origin <> 'Manual' or manual_reason is not null");
                    table.CheckConstraint("ck_journeys_sub_state", "running_sub_state is null or lifecycle = 'Running'");
                    table.CheckConstraint("ck_journeys_times", "scheduled_arrival_utc > scheduled_departure_utc");
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "journeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "jsonb", nullable: false),
                    OccurredOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProcessedOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeadLetteredOnUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Attempts = table.Column<int>(type: "integer", nullable: false),
                    Error = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_messages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "vehicles",
                schema: "journeys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    kind = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicles", x => x.id);
                    table.CheckConstraint("ck_vehicles_kind", "kind in ('Trainset','Locomotive')");
                });

            migrationBuilder.CreateTable(
                name: "driver_assignments",
                schema: "journeys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    journey_id = table.Column<Guid>(type: "uuid", nullable: false),
                    duty_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    assigned_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_driver_assignments", x => x.id);
                    table.CheckConstraint("ck_driver_assignments_period", "valid_to is null or valid_to > valid_from");
                    table.ForeignKey(
                        name: "FK_driver_assignments_crew_duties_duty_id",
                        column: x => x.duty_id,
                        principalSchema: "journeys",
                        principalTable: "crew_duties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_driver_assignments_journeys_journey_id",
                        column: x => x.journey_id,
                        principalSchema: "journeys",
                        principalTable: "journeys",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "journey_stops",
                schema: "journeys",
                columns: table => new
                {
                    journey_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    station_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    state = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    scheduled_arrival_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    scheduled_departure_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    estimated_arrival_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    estimated_departure_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    actual_arrival_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    actual_departure_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    estimate_source = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    dwell_minutes = table.Column<int>(type: "integer", nullable: false),
                    platform_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    platform_state = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    platform_original_code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    platform_changed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    platform_changed_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journey_stops", x => new { x.journey_id, x.sequence });
                    table.CheckConstraint("ck_journey_stops_dwell", "dwell_minutes >= 0");
                    table.CheckConstraint("ck_journey_stops_kind", "kind in ('Stop','PickUpOnly','SetDownOnly','Passing')");
                    table.CheckConstraint("ck_journey_stops_platform_state", "platform_state in ('TBA','Assigned','Changed')");
                    table.CheckConstraint("ck_journey_stops_sequence", "sequence >= 1");
                    table.CheckConstraint("ck_journey_stops_source", "estimate_source in ('Confirmed','Live','Scheduled')");
                    table.CheckConstraint("ck_journey_stops_state", "state in ('Pending','Arrived','Departed','Skipped','Cancelled')");
                    table.ForeignKey(
                        name: "FK_journey_stops_journeys_journey_id",
                        column: x => x.journey_id,
                        principalSchema: "journeys",
                        principalTable: "journeys",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vehicle_assignments",
                schema: "journeys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    journey_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    valid_to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    assigned_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_assignments", x => x.id);
                    table.CheckConstraint("ck_vehicle_assignments_period", "valid_to is null or valid_to > valid_from");
                    table.ForeignKey(
                        name: "FK_vehicle_assignments_journeys_journey_id",
                        column: x => x.journey_id,
                        principalSchema: "journeys",
                        principalTable: "journeys",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_vehicle_assignments_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalSchema: "journeys",
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_occurred_at",
                schema: "journeys",
                table: "audit_entries",
                column: "occurred_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_target_type_target_id",
                schema: "journeys",
                table: "audit_entries",
                columns: new[] { "target_type", "target_id" });

            migrationBuilder.CreateIndex(
                name: "IX_crew_duties_driver_subject_service_date",
                schema: "journeys",
                table: "crew_duties",
                columns: new[] { "driver_subject", "service_date" });

            migrationBuilder.CreateIndex(
                name: "IX_crew_duties_handover_from_duty_id",
                schema: "journeys",
                table: "crew_duties",
                column: "handover_from_duty_id");

            migrationBuilder.CreateIndex(
                name: "IX_driver_assignments_duty_id",
                schema: "journeys",
                table: "driver_assignments",
                column: "duty_id");

            migrationBuilder.CreateIndex(
                name: "IX_driver_assignments_journey_id",
                schema: "journeys",
                table: "driver_assignments",
                column: "journey_id");

            migrationBuilder.CreateIndex(
                name: "ix_journey_stops_arrivals",
                schema: "journeys",
                table: "journey_stops",
                columns: new[] { "station_code", "scheduled_arrival_utc" },
                filter: "kind <> 'Passing'");

            migrationBuilder.CreateIndex(
                name: "ix_journey_stops_board",
                schema: "journeys",
                table: "journey_stops",
                columns: new[] { "station_code", "scheduled_departure_utc" },
                filter: "kind <> 'Passing'");

            migrationBuilder.CreateIndex(
                name: "ix_journeys_active",
                schema: "journeys",
                table: "journeys",
                column: "scheduled_departure_utc",
                filter: "lifecycle in ('Planned', 'Ready', 'Running', 'Interrupted')");

            migrationBuilder.CreateIndex(
                name: "ix_journeys_date_lifecycle",
                schema: "journeys",
                table: "journeys",
                columns: new[] { "service_date", "lifecycle" });

            migrationBuilder.CreateIndex(
                name: "ux_journeys_service_date",
                schema: "journeys",
                table: "journeys",
                columns: new[] { "service_id", "service_date" },
                unique: true,
                filter: "service_id is not null");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_OccurredOnUtc",
                schema: "journeys",
                table: "outbox_messages",
                column: "OccurredOnUtc",
                filter: "\"ProcessedOnUtc\" IS NULL AND \"DeadLetteredOnUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_assignments_journey_id",
                schema: "journeys",
                table: "vehicle_assignments",
                column: "journey_id");

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_assignments_vehicle_id",
                schema: "journeys",
                table: "vehicle_assignments",
                column: "vehicle_id");

            migrationBuilder.CreateIndex(
                name: "IX_vehicles_code",
                schema: "journeys",
                table: "vehicles",
                column: "code",
                unique: true);

            // One vehicle per journey at a time, and a vehicle is not on two journeys at once (REQ-LIVE-010).
            migrationBuilder.Sql("ALTER TABLE journeys.vehicle_assignments ADD CONSTRAINT ex_vehicle_assignments_journey EXCLUDE USING gist (journey_id WITH =, tstzrange(valid_from, valid_to) WITH &&);");
            migrationBuilder.Sql("ALTER TABLE journeys.vehicle_assignments ADD CONSTRAINT ex_vehicle_assignments_vehicle EXCLUDE USING gist (vehicle_id WITH =, tstzrange(valid_from, valid_to) WITH &&);");

            // One duty on a journey at a time, so the outgoing duty closes before the incoming opens (REQ-DRV-009).
            migrationBuilder.Sql("ALTER TABLE journeys.driver_assignments ADD CONSTRAINT ex_driver_assignments_journey EXCLUDE USING gist (journey_id WITH =, tstzrange(valid_from, valid_to) WITH &&);");

            // A driver's rostered windows cannot overlap (double-booking is rejected).
            migrationBuilder.Sql("ALTER TABLE journeys.crew_duties ADD CONSTRAINT ex_crew_duties_driver EXCLUDE USING gist (driver_subject WITH =, tstzrange(rostered_from, rostered_to) WITH &&);");

            // The audit log is append-only (REQ-OPS-011).
            migrationBuilder.Sql(@"CREATE FUNCTION journeys.forbid_update_delete() RETURNS trigger AS $$
BEGIN
    RAISE EXCEPTION '% on %.% is not allowed: the table is append-only', TG_OP, TG_TABLE_SCHEMA, TG_TABLE_NAME;
END;
$$ LANGUAGE plpgsql;");
            migrationBuilder.Sql("CREATE TRIGGER trg_audit_entries_append_only BEFORE UPDATE OR DELETE ON journeys.audit_entries FOR EACH ROW EXECUTE FUNCTION journeys.forbid_update_delete();");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_audit_entries_append_only ON journeys.audit_entries;");
            migrationBuilder.DropTable(
                name: "audit_entries",
                schema: "journeys");

            migrationBuilder.DropTable(
                name: "driver_assignments",
                schema: "journeys");

            migrationBuilder.DropTable(
                name: "journey_stops",
                schema: "journeys");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "journeys");

            migrationBuilder.DropTable(
                name: "vehicle_assignments",
                schema: "journeys");

            migrationBuilder.DropTable(
                name: "crew_duties",
                schema: "journeys");

            migrationBuilder.DropTable(
                name: "journeys",
                schema: "journeys");

            migrationBuilder.DropTable(
                name: "vehicles",
                schema: "journeys");

            migrationBuilder.Sql("DROP FUNCTION IF EXISTS journeys.forbid_update_delete();");
        }
    }
}
