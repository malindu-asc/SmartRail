using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartRail.Modules.Journeys.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DriverDelayReason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "delay_reason_at",
                schema: "journeys",
                table: "journeys",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "delay_reason_code",
                schema: "journeys",
                table: "journeys",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "delay_reason_at",
                schema: "journeys",
                table: "journeys");

            migrationBuilder.DropColumn(
                name: "delay_reason_code",
                schema: "journeys",
                table: "journeys");
        }
    }
}
