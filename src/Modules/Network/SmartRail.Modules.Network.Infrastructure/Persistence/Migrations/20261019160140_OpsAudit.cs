using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SmartRail.Modules.Network.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OpsAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_entries",
                schema: "network",
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

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_occurred_at",
                schema: "network",
                table: "audit_entries",
                column: "occurred_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_target_type_target_id",
                schema: "network",
                table: "audit_entries",
                columns: new[] { "target_type", "target_id" });

            // The audit log is append-only (REQ-OPS-011).
            migrationBuilder.Sql(@"CREATE FUNCTION network.forbid_update_delete() RETURNS trigger AS $$
BEGIN
    RAISE EXCEPTION '% on %.% is not allowed: the table is append-only', TG_OP, TG_TABLE_SCHEMA, TG_TABLE_NAME;
END;
$$ LANGUAGE plpgsql;");
            migrationBuilder.Sql("CREATE TRIGGER trg_audit_entries_append_only BEFORE UPDATE OR DELETE ON network.audit_entries FOR EACH ROW EXECUTE FUNCTION network.forbid_update_delete();");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TRIGGER IF EXISTS trg_audit_entries_append_only ON network.audit_entries;");
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS network.forbid_update_delete();");
            migrationBuilder.DropTable(
                name: "audit_entries",
                schema: "network");
        }
    }
}
