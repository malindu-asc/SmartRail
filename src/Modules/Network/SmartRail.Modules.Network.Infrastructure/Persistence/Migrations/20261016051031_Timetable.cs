using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartRail.Modules.Network.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Timetable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "service_calendars",
                schema: "network",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    monday = table.Column<bool>(type: "boolean", nullable: false),
                    tuesday = table.Column<bool>(type: "boolean", nullable: false),
                    wednesday = table.Column<bool>(type: "boolean", nullable: false),
                    thursday = table.Column<bool>(type: "boolean", nullable: false),
                    friday = table.Column<bool>(type: "boolean", nullable: false),
                    saturday = table.Column<bool>(type: "boolean", nullable: false),
                    sunday = table.Column<bool>(type: "boolean", nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: false),
                    valid_to = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_calendars", x => x.id);
                    table.CheckConstraint("ck_service_calendars_range", "valid_to is null or valid_to >= valid_from");
                });

            migrationBuilder.CreateTable(
                name: "timetable_versions",
                schema: "network",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    effective_from = table.Column<DateOnly>(type: "date", nullable: true),
                    author = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    approved_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_timetable_versions", x => x.id);
                    table.CheckConstraint("ck_timetable_versions_approver", "status not in ('Published', 'Superseded') or (approved_by is not null and approved_by <> author)");
                    table.CheckConstraint("ck_timetable_versions_effective", "status in ('Draft', 'InReview') or effective_from is not null");
                    table.CheckConstraint("ck_timetable_versions_status", "status in ('Draft', 'InReview', 'Published', 'Superseded')");
                });

            migrationBuilder.CreateTable(
                name: "calendar_exceptions",
                schema: "network",
                columns: table => new
                {
                    calendar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    kind = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_calendar_exceptions", x => new { x.calendar_id, x.date });
                    table.CheckConstraint("ck_calendar_exceptions_kind", "kind in ('Removed', 'Added')");
                    table.ForeignKey(
                        name: "FK_calendar_exceptions_service_calendars_calendar_id",
                        column: x => x.calendar_id,
                        principalSchema: "network",
                        principalTable: "service_calendars",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "services",
                schema: "network",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    timetable_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    route_id = table.Column<Guid>(type: "uuid", nullable: false),
                    calendar_id = table.Column<Guid>(type: "uuid", nullable: false),
                    train_number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    direction = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    service_category = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_services", x => x.id);
                    table.CheckConstraint("ck_services_category", "service_category in ('Regional', 'Intercity', 'HighSpeed')");
                    table.CheckConstraint("ck_services_direction", "direction in ('Forward', 'Reverse')");
                    table.ForeignKey(
                        name: "FK_services_routes_route_id",
                        column: x => x.route_id,
                        principalSchema: "network",
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_services_service_calendars_calendar_id",
                        column: x => x.calendar_id,
                        principalSchema: "network",
                        principalTable: "service_calendars",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_services_timetable_versions_timetable_version_id",
                        column: x => x.timetable_version_id,
                        principalSchema: "network",
                        principalTable: "timetable_versions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_stops",
                schema: "network",
                columns: table => new
                {
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    station_id = table.Column<Guid>(type: "uuid", nullable: false),
                    arrival_minutes = table.Column<int>(type: "integer", nullable: true),
                    departure_minutes = table.Column<int>(type: "integer", nullable: true),
                    default_platform_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_stops", x => new { x.service_id, x.sequence });
                    table.CheckConstraint("ck_service_stops_arrival", "arrival_minutes between 0 and 2879");
                    table.CheckConstraint("ck_service_stops_departure", "departure_minutes between 0 and 2879");
                    table.CheckConstraint("ck_service_stops_has_time", "arrival_minutes is not null or departure_minutes is not null");
                    table.CheckConstraint("ck_service_stops_kind", "kind in ('Stop', 'PickUpOnly', 'SetDownOnly', 'Passing')");
                    table.CheckConstraint("ck_service_stops_order", "arrival_minutes is null or departure_minutes is null or arrival_minutes <= departure_minutes");
                    table.CheckConstraint("ck_service_stops_sequence", "sequence >= 1");
                    table.ForeignKey(
                        name: "FK_service_stops_platforms_default_platform_id_station_id",
                        columns: x => new { x.default_platform_id, x.station_id },
                        principalSchema: "network",
                        principalTable: "platforms",
                        principalColumns: new[] { "id", "station_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_stops_services_service_id",
                        column: x => x.service_id,
                        principalSchema: "network",
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_service_stops_stations_station_id",
                        column: x => x.station_id,
                        principalSchema: "network",
                        principalTable: "stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_service_calendars_code",
                schema: "network",
                table: "service_calendars",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_stops_default_platform_id_station_id",
                schema: "network",
                table: "service_stops",
                columns: new[] { "default_platform_id", "station_id" });

            migrationBuilder.CreateIndex(
                name: "IX_service_stops_service_id_station_id",
                schema: "network",
                table: "service_stops",
                columns: new[] { "service_id", "station_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_service_stops_station_id_departure_minutes",
                schema: "network",
                table: "service_stops",
                columns: new[] { "station_id", "departure_minutes" });

            migrationBuilder.CreateIndex(
                name: "IX_services_calendar_id",
                schema: "network",
                table: "services",
                column: "calendar_id");

            migrationBuilder.CreateIndex(
                name: "IX_services_route_id",
                schema: "network",
                table: "services",
                column: "route_id");

            migrationBuilder.CreateIndex(
                name: "IX_services_timetable_version_id_train_number",
                schema: "network",
                table: "services",
                columns: new[] { "timetable_version_id", "train_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_timetable_effective",
                schema: "network",
                table: "timetable_versions",
                column: "effective_from",
                unique: true,
                filter: "status in ('Published', 'Superseded')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "calendar_exceptions",
                schema: "network");

            migrationBuilder.DropTable(
                name: "service_stops",
                schema: "network");

            migrationBuilder.DropTable(
                name: "services",
                schema: "network");

            migrationBuilder.DropTable(
                name: "service_calendars",
                schema: "network");

            migrationBuilder.DropTable(
                name: "timetable_versions",
                schema: "network");
        }
    }
}
