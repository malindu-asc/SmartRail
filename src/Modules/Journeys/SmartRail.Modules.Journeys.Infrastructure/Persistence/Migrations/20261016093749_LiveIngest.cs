using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SmartRail.Modules.Journeys.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class LiveIngest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "journey_events",
                schema: "journeys",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    journey_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    stop_sequence = table.Column<int>(type: "integer", nullable: false),
                    source_kind = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_rank = table.Column<int>(type: "integer", nullable: false),
                    occurred_at_source = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    device_sequence = table.Column<long>(type: "bigint", nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    disposition = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    disposition_reason = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    superseded_by_event_id = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_journey_events", x => x.id);
                    table.CheckConstraint("ck_journey_events_disposition", "disposition in ('Applied','Superseded','Rejected','HeldForCorrection','Duplicate','Orphaned','OutsideDutyWindow')");
                    table.CheckConstraint("ck_journey_events_rank", "source_rank >= 1");
                    table.CheckConstraint("ck_journey_events_source_kind", "source_kind in ('Feed','Driver','Controller','System')");
                    table.CheckConstraint("ck_journey_events_type", "event_type in ('Arrived','Departed','Passed')");
                    table.ForeignKey(
                        name: "FK_journey_events_journey_events_superseded_by_event_id",
                        column: x => x.superseded_by_event_id,
                        principalSchema: "journeys",
                        principalTable: "journey_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_journey_events_journeys_journey_id",
                        column: x => x.journey_id,
                        principalSchema: "journeys",
                        principalTable: "journeys",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "telemetry_readings",
                schema: "journeys",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    source_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    recorded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    sequence = table.Column<long>(type: "bigint", nullable: false),
                    section_from_station_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    section_to_station_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    progress = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    speed_kmh = table.Column<decimal>(type: "numeric(5,1)", precision: 5, scale: 1, nullable: true),
                    resolved_journey_id = table.Column<Guid>(type: "uuid", nullable: true),
                    accepted = table.Column<bool>(type: "boolean", nullable: false),
                    reject_reason = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    legal_hold = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_telemetry_readings", x => x.id);
                    table.CheckConstraint("ck_telemetry_progress", "progress is null or (progress >= 0 and progress <= 1)");
                    table.CheckConstraint("ck_telemetry_speed", "speed_kmh is null or speed_kmh >= 0");
                });

            migrationBuilder.CreateIndex(
                name: "ix_events_fact",
                schema: "journeys",
                table: "journey_events",
                columns: new[] { "journey_id", "stop_sequence", "event_type" },
                filter: "disposition = 'Applied'");

            migrationBuilder.CreateIndex(
                name: "ix_events_journey",
                schema: "journeys",
                table: "journey_events",
                columns: new[] { "journey_id", "received_at" });

            migrationBuilder.CreateIndex(
                name: "IX_journey_events_superseded_by_event_id",
                schema: "journeys",
                table: "journey_events",
                column: "superseded_by_event_id");

            migrationBuilder.CreateIndex(
                name: "ux_events_source_sequence",
                schema: "journeys",
                table: "journey_events",
                columns: new[] { "source_id", "device_sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_telemetry_purge",
                schema: "journeys",
                table: "telemetry_readings",
                column: "received_at",
                filter: "legal_hold = false");

            migrationBuilder.CreateIndex(
                name: "ix_telemetry_vehicle",
                schema: "journeys",
                table: "telemetry_readings",
                columns: new[] { "vehicle_code", "recorded_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ux_telemetry_source_sequence",
                schema: "journeys",
                table: "telemetry_readings",
                columns: new[] { "source_id", "sequence" },
                unique: true);

            // The event history is never deleted: a rejected or superseded value stays visible (REQ-LIVE-006). A better-ranked
            // source may still mark an earlier event as superseded, so updates are allowed.
            migrationBuilder.Sql(@"CREATE FUNCTION journeys.forbid_delete() RETURNS trigger AS $$
BEGIN
    RAISE EXCEPTION 'DELETE on %.% is not allowed: the table is append-only', TG_TABLE_SCHEMA, TG_TABLE_NAME;
END;
$$ LANGUAGE plpgsql;");
            migrationBuilder.Sql("CREATE TRIGGER trg_journey_events_no_delete BEFORE DELETE ON journeys.journey_events FOR EACH ROW EXECUTE FUNCTION journeys.forbid_delete();");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_journey_events_no_delete ON journeys.journey_events;");
            migrationBuilder.DropTable(
                name: "journey_events",
                schema: "journeys");

            migrationBuilder.DropTable(
                name: "telemetry_readings",
                schema: "journeys");

            migrationBuilder.Sql("DROP FUNCTION IF EXISTS journeys.forbid_delete();");
        }
    }
}
