using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UTOP.Booking.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "utop_booking");

            migrationBuilder.CreateTable(
                name: "bookings",
                schema: "utop_booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    mode = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    route = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    category = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    operator_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    group_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    pilgrimage_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    amendment_version = table.Column<int>(type: "integer", nullable: false),
                    row_version = table.Column<int>(type: "integer", nullable: false),
                    adults = table.Column<int>(type: "integer", nullable: false),
                    children = table.Column<int>(type: "integer", nullable: false),
                    infants = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bookings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "idempotency_keys",
                schema: "utop_booking",
                columns: table => new
                {
                    key_hash = table.Column<string>(type: "character(64)", nullable: false),
                    booking_id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_keys", x => x.key_hash);
                });

            migrationBuilder.CreateTable(
                name: "outbox_events",
                schema: "utop_booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_outbox_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "itineraries",
                schema: "utop_booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    departure_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    arrival_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    departure_airport = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    departure_city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    departure_country = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    arrival_airport = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    arrival_city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    arrival_country = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    carrier_reference = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    service_class = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_itineraries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_itineraries_bookings_booking_id",
                        column: x => x.booking_id,
                        principalSchema: "utop_booking",
                        principalTable: "bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "passengers",
                schema: "utop_booking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: false),
                    passenger_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: false),
                    document_number = table.Column<string>(type: "text", nullable: true),
                    nationality = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    booking_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_passengers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_passengers_bookings_booking_id",
                        column: x => x.booking_id,
                        principalSchema: "utop_booking",
                        principalTable: "bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_bookings_group_id",
                schema: "utop_booking",
                table: "bookings",
                column: "group_id",
                filter: "group_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_operator",
                schema: "utop_booking",
                table: "bookings",
                column: "operator_id");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_pilgrimage_id",
                schema: "utop_booking",
                table: "bookings",
                column: "pilgrimage_id",
                filter: "pilgrimage_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_bookings_status",
                schema: "utop_booking",
                table: "bookings",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "uq_bookings_booking_id",
                schema: "utop_booking",
                table: "bookings",
                column: "booking_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_itineraries_booking_id",
                schema: "utop_booking",
                table: "itineraries",
                column: "booking_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_itineraries_departure",
                schema: "utop_booking",
                table: "itineraries",
                column: "departure_utc");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_unpublished",
                schema: "utop_booking",
                table: "outbox_events",
                column: "created_at",
                filter: "published_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_passengers_booking_id",
                schema: "utop_booking",
                table: "passengers",
                column: "booking_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "idempotency_keys",
                schema: "utop_booking");

            migrationBuilder.DropTable(
                name: "itineraries",
                schema: "utop_booking");

            migrationBuilder.DropTable(
                name: "outbox_events",
                schema: "utop_booking");

            migrationBuilder.DropTable(
                name: "passengers",
                schema: "utop_booking");

            migrationBuilder.DropTable(
                name: "bookings",
                schema: "utop_booking");
        }
    }
}
