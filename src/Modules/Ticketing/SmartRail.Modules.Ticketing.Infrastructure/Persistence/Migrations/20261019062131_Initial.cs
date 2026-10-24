using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartRail.Modules.Ticketing.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ticketing");

            migrationBuilder.CreateTable(
                name: "bookings",
                schema: "ticketing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    reference = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    booking_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    user_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    contact_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    journey_id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_date = table.Column<DateOnly>(type: "date", nullable: false),
                    route_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    train_number = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    from_station_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    to_station_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    from_index = table.Column<int>(type: "integer", nullable: false),
                    to_index = table.Column<int>(type: "integer", nullable: false),
                    departure_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    arrival_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    adults = table.Column<int>(type: "integer", nullable: false),
                    children = table.Column<int>(type: "integer", nullable: false),
                    seniors = table.Column<int>(type: "integer", nullable: false),
                    needs_help = table.Column<bool>(type: "boolean", nullable: false),
                    fare_total = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    seats = table.Column<string>(type: "jsonb", nullable: false),
                    created_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookings", x => x.id);
                    table.CheckConstraint("ck_bookings_passengers", "adults >= 0 and children >= 0 and seniors >= 0 and adults + children + seniors between 1 and 9");
                    table.CheckConstraint("ck_bookings_segment", "to_index > from_index");
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "ticketing",
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
                name: "seat_holds",
                schema: "ticketing",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    journey_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_index = table.Column<int>(type: "integer", nullable: false),
                    to_index = table.Column<int>(type: "integer", nullable: false),
                    seats = table.Column<string>(type: "jsonb", nullable: false),
                    expires_at_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seat_holds", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_journey",
                schema: "ticketing",
                table: "bookings",
                column: "journey_id");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_user",
                schema: "ticketing",
                table: "bookings",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_bookings_booking_code",
                schema: "ticketing",
                table: "bookings",
                column: "booking_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_bookings_reference",
                schema: "ticketing",
                table: "bookings",
                column: "reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_outbox_messages_OccurredOnUtc",
                schema: "ticketing",
                table: "outbox_messages",
                column: "OccurredOnUtc",
                filter: "\"ProcessedOnUtc\" IS NULL AND \"DeadLetteredOnUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_seat_holds_journey",
                schema: "ticketing",
                table: "seat_holds",
                column: "journey_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bookings",
                schema: "ticketing");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "ticketing");

            migrationBuilder.DropTable(
                name: "seat_holds",
                schema: "ticketing");
        }
    }
}
