using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SmartRail.Modules.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OpsConsole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "operations",
                table: "disruptions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(10)",
                oldMaxLength: 10);

            migrationBuilder.AddColumn<string>(
                name: "rejected_by",
                schema: "operations",
                table: "disruptions",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rejection_comment",
                schema: "operations",
                table: "disruptions",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "starts_at_utc",
                schema: "operations",
                table: "disruptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "submitted_at_utc",
                schema: "operations",
                table: "disruptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "type",
                schema: "operations",
                table: "disruptions",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "assistance_requests",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    driver_subject = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    journey_id = table.Column<Guid>(type: "uuid", nullable: true),
                    train_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    station_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    requested_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    handled_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    handled_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    outcome = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assistance_requests", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "audit_entries",
                schema: "operations",
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
                name: "control_messages",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    driver_subject = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    journey_id = table.Column<Guid>(type: "uuid", nullable: true),
                    train_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    unit_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    incident_id = table.Column<Guid>(type: "uuid", nullable: true),
                    template = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    body = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    requires_ack = table.Column<bool>(type: "boolean", nullable: false),
                    valid_until_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    sent_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    sent_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    delivered_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    acknowledged_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    resent_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_control_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "incidents",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    severity = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    status = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    owner_subject = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    opened_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    opened_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    reported_by = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    station_codes = table.Column<string>(type: "jsonb", nullable: false),
                    disruption_id = table.Column<Guid>(type: "uuid", nullable: true),
                    resolved_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    resolved_by = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    resolution_note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incidents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "incident_entries",
                schema: "operations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    incident_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    text = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    detail = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    author = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_incident_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_incident_entries_incidents_incident_id",
                        column: x => x.incident_id,
                        principalSchema: "operations",
                        principalTable: "incidents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_assistance_requests_status",
                schema: "operations",
                table: "assistance_requests",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_occurred_at",
                schema: "operations",
                table: "audit_entries",
                column: "occurred_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_target_type_target_id",
                schema: "operations",
                table: "audit_entries",
                columns: new[] { "target_type", "target_id" });

            migrationBuilder.CreateIndex(
                name: "ix_control_messages_driver_sent",
                schema: "operations",
                table: "control_messages",
                columns: new[] { "driver_subject", "sent_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_control_messages_incident",
                schema: "operations",
                table: "control_messages",
                column: "incident_id");

            migrationBuilder.CreateIndex(
                name: "ix_incident_entries_incident_at",
                schema: "operations",
                table: "incident_entries",
                columns: new[] { "incident_id", "at" });

            migrationBuilder.CreateIndex(
                name: "ix_incidents_status",
                schema: "operations",
                table: "incidents",
                column: "status");

            // A network-wide notice now waits for a second person; older drafts of that kind move to the approval queue.
            migrationBuilder.Sql("UPDATE operations.disruptions SET status = 'AwaitingApproval' WHERE status = 'Draft' AND network_wide;");

            // The audit log is append-only (REQ-OPS-011).
            migrationBuilder.Sql(@"CREATE FUNCTION operations.forbid_update_delete() RETURNS trigger AS $$
BEGIN
    RAISE EXCEPTION '% on %.% is not allowed: the table is append-only', TG_OP, TG_TABLE_SCHEMA, TG_TABLE_NAME;
END;
$$ LANGUAGE plpgsql;");
            migrationBuilder.Sql("CREATE TRIGGER trg_audit_entries_append_only BEFORE UPDATE OR DELETE ON operations.audit_entries FOR EACH ROW EXECUTE FUNCTION operations.forbid_update_delete();");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_audit_entries_append_only ON operations.audit_entries;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS operations.forbid_update_delete();");

            migrationBuilder.DropTable(
                name: "assistance_requests",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "audit_entries",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "control_messages",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "incident_entries",
                schema: "operations");

            migrationBuilder.DropTable(
                name: "incidents",
                schema: "operations");

            migrationBuilder.DropColumn(
                name: "rejected_by",
                schema: "operations",
                table: "disruptions");

            migrationBuilder.DropColumn(
                name: "rejection_comment",
                schema: "operations",
                table: "disruptions");

            migrationBuilder.DropColumn(
                name: "starts_at_utc",
                schema: "operations",
                table: "disruptions");

            migrationBuilder.DropColumn(
                name: "submitted_at_utc",
                schema: "operations",
                table: "disruptions");

            migrationBuilder.DropColumn(
                name: "type",
                schema: "operations",
                table: "disruptions");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                schema: "operations",
                table: "disruptions",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }
    }
}
