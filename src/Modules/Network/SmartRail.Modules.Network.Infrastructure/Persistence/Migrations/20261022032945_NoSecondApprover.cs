using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartRail.Modules.Network.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class NoSecondApprover : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_timetable_versions_approver",
                schema: "network",
                table: "timetable_versions");

            migrationBuilder.AddCheckConstraint(
                name: "ck_timetable_versions_approver",
                schema: "network",
                table: "timetable_versions",
                sql: "status not in ('Published', 'Superseded') or approved_by is not null");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_timetable_versions_approver",
                schema: "network",
                table: "timetable_versions");

            migrationBuilder.AddCheckConstraint(
                name: "ck_timetable_versions_approver",
                schema: "network",
                table: "timetable_versions",
                sql: "status not in ('Published', 'Superseded') or (approved_by is not null and approved_by <> author)");
        }
    }
}
