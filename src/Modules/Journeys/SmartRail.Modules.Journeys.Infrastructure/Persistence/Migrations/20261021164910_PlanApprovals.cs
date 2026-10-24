using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartRail.Modules.Journeys.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PlanApprovals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "plan_approvals",
                schema: "journeys",
                columns: table => new
                {
                    service_date = table.Column<DateOnly>(type: "date", nullable: false),
                    approved_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plan_approvals", x => x.service_date);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "plan_approvals",
                schema: "journeys");
        }
    }
}
