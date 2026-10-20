using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartRail.Modules.Journeys.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OpsConsole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "controller_note",
                schema: "journeys",
                table: "journeys",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "end_reason_code",
                schema: "journeys",
                table: "journeys",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "vehicle_unavailability",
                schema: "journeys",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehicle_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    to = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    reason = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    created_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ended_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vehicle_unavailability", x => x.id);
                    table.CheckConstraint("ck_vehicle_unavailability_period", "\"to\" is null or \"to\" > \"from\"");
                    table.ForeignKey(
                        name: "FK_vehicle_unavailability_vehicles_vehicle_id",
                        column: x => x.vehicle_id,
                        principalSchema: "journeys",
                        principalTable: "vehicles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_vehicle_unavailability_vehicle_id_from",
                schema: "journeys",
                table: "vehicle_unavailability",
                columns: new[] { "vehicle_id", "from" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "vehicle_unavailability",
                schema: "journeys");

            migrationBuilder.DropColumn(
                name: "controller_note",
                schema: "journeys",
                table: "journeys");

            migrationBuilder.DropColumn(
                name: "end_reason_code",
                schema: "journeys",
                table: "journeys");
        }
    }
}
