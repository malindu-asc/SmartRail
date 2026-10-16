using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartRail.Modules.Network.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "network");

            migrationBuilder.CreateTable(
                name: "languages",
                schema: "network",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_languages", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "network",
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
                name: "routes",
                schema: "network",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_routes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stations",
                schema: "network",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    facilities = table.Column<string[]>(type: "text[]", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stations", x => x.id);
                    table.CheckConstraint("ck_stations_code", "code ~ '^[A-Z]{3,5}$'");
                });

            migrationBuilder.CreateTable(
                name: "platforms",
                schema: "network",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    station_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    is_available = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_platforms", x => x.id);
                    table.UniqueConstraint("AK_platforms_id_station_id", x => new { x.id, x.station_id });
                    table.ForeignKey(
                        name: "FK_platforms_stations_station_id",
                        column: x => x.station_id,
                        principalSchema: "network",
                        principalTable: "stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "station_translations",
                schema: "network",
                columns: table => new
                {
                    station_id = table.Column<Guid>(type: "uuid", nullable: false),
                    language_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_station_translations", x => new { x.station_id, x.language_code });
                    table.ForeignKey(
                        name: "FK_station_translations_languages_language_code",
                        column: x => x.language_code,
                        principalSchema: "network",
                        principalTable: "languages",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_station_translations_stations_station_id",
                        column: x => x.station_id,
                        principalSchema: "network",
                        principalTable: "stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "route_stops",
                schema: "network",
                columns: table => new
                {
                    route_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sequence = table.Column<int>(type: "integer", nullable: false),
                    station_id = table.Column<Guid>(type: "uuid", nullable: false),
                    default_platform_id = table.Column<Guid>(type: "uuid", nullable: true),
                    minutes_from_previous = table.Column<int>(type: "integer", nullable: false),
                    dwell_minutes = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_route_stops", x => new { x.route_id, x.sequence });
                    table.CheckConstraint("ck_route_stops_dwell", "dwell_minutes >= 0");
                    table.CheckConstraint("ck_route_stops_kind", "kind in ('Stop', 'PickUpOnly', 'SetDownOnly', 'Passing')");
                    table.CheckConstraint("ck_route_stops_minutes", "minutes_from_previous >= 0");
                    table.CheckConstraint("ck_route_stops_sequence", "sequence >= 1");
                    table.ForeignKey(
                        name: "FK_route_stops_platforms_default_platform_id_station_id",
                        columns: x => new { x.default_platform_id, x.station_id },
                        principalSchema: "network",
                        principalTable: "platforms",
                        principalColumns: new[] { "id", "station_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_route_stops_routes_route_id",
                        column: x => x.route_id,
                        principalSchema: "network",
                        principalTable: "routes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_route_stops_stations_station_id",
                        column: x => x.station_id,
                        principalSchema: "network",
                        principalTable: "stations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_languages_is_default",
                schema: "network",
                table: "languages",
                column: "is_default",
                unique: true,
                filter: "is_default");

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_OccurredOnUtc",
                schema: "network",
                table: "outbox_messages",
                column: "OccurredOnUtc",
                filter: "\"ProcessedOnUtc\" IS NULL AND \"DeadLetteredOnUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_platforms_station_id_code",
                schema: "network",
                table: "platforms",
                columns: new[] { "station_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_route_stops_default_platform_id_station_id",
                schema: "network",
                table: "route_stops",
                columns: new[] { "default_platform_id", "station_id" });

            migrationBuilder.CreateIndex(
                name: "IX_route_stops_route_id_station_id",
                schema: "network",
                table: "route_stops",
                columns: new[] { "route_id", "station_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_route_stops_station_id",
                schema: "network",
                table: "route_stops",
                column: "station_id");

            migrationBuilder.CreateIndex(
                name: "IX_routes_code",
                schema: "network",
                table: "routes",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_station_translations_language_code",
                schema: "network",
                table: "station_translations",
                column: "language_code");

            migrationBuilder.CreateIndex(
                name: "IX_stations_code",
                schema: "network",
                table: "stations",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stations_name",
                schema: "network",
                table: "stations",
                column: "name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "network");

            migrationBuilder.DropTable(
                name: "route_stops",
                schema: "network");

            migrationBuilder.DropTable(
                name: "station_translations",
                schema: "network");

            migrationBuilder.DropTable(
                name: "platforms",
                schema: "network");

            migrationBuilder.DropTable(
                name: "routes",
                schema: "network");

            migrationBuilder.DropTable(
                name: "languages",
                schema: "network");

            migrationBuilder.DropTable(
                name: "stations",
                schema: "network");
        }
    }
}
