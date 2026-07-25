using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SeatFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: false),
                    Description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    AgeRestriction = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_events", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "venues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Address = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_venues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "halls",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    VenueId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Capacity = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_halls", x => x.Id);
                    table.ForeignKey(
                        name: "FK_halls_venues_VenueId",
                        column: x => x.VenueId,
                        principalTable: "venues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "event_sessions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    HallId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    BookingOpensAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    BookingClosesAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsCancelled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_sessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_event_sessions_events_EventId",
                        column: x => x.EventId,
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_event_sessions_halls_HallId",
                        column: x => x.HallId,
                        principalTable: "halls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "seats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HallId = table.Column<Guid>(type: "uuid", nullable: false),
                    RowLabel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_seats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_seats_halls_HallId",
                        column: x => x.HallId,
                        principalTable: "halls",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "reservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ExpiredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reservations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reservations_event_sessions_EventSessionId",
                        column: x => x.EventSessionId,
                        principalTable: "event_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "payments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ExternalReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_payments_reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "session_seats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventSessionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SeatId = table.Column<Guid>(type: "uuid", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReservedUntilUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_session_seats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_session_seats_event_sessions_EventSessionId",
                        column: x => x.EventSessionId,
                        principalTable: "event_sessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_session_seats_reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_session_seats_seats_SeatId",
                        column: x => x.SeatId,
                        principalTable: "seats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "reservation_seats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReservationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SessionSeatId = table.Column<Guid>(type: "uuid", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reservation_seats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_reservation_seats_reservations_ReservationId",
                        column: x => x.ReservationId,
                        principalTable: "reservations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_reservation_seats_session_seats_SessionSeatId",
                        column: x => x.SessionSeatId,
                        principalTable: "session_seats",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "events",
                columns: new[] { "Id", "AgeRestriction", "Category", "Description", "Title" },
                values: new object[] { new Guid("33333333-3333-3333-3333-333333333333"), 12, "Concert", "Demonstration event used for local development.", "SeatFlow Demo Concert" });

            migrationBuilder.InsertData(
                table: "venues",
                columns: new[] { "Id", "Address", "Description", "Name" },
                values: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), "100 Demo Avenue", "Demonstration venue for the SeatFlow API.", "SeatFlow Arena" });

            migrationBuilder.InsertData(
                table: "halls",
                columns: new[] { "Id", "Capacity", "Name", "VenueId" },
                values: new object[] { new Guid("22222222-2222-2222-2222-222222222222"), 12, "Main Hall", new Guid("11111111-1111-1111-1111-111111111111") });

            migrationBuilder.InsertData(
                table: "event_sessions",
                columns: new[] { "Id", "BookingClosesAtUtc", "BookingOpensAtUtc", "CancelledAtUtc", "EventId", "HallId", "StartsAtUtc" },
                values: new object[] { new Guid("44444444-4444-4444-4444-444444444444"), new DateTimeOffset(new DateTime(2026, 12, 15, 18, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 7, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), null, new Guid("33333333-3333-3333-3333-333333333333"), new Guid("22222222-2222-2222-2222-222222222222"), new DateTimeOffset(new DateTime(2026, 12, 15, 19, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.InsertData(
                table: "seats",
                columns: new[] { "Id", "Category", "HallId", "Number", "RowLabel" },
                values: new object[,]
                {
                    { new Guid("50000000-0000-0000-0000-000000000001"), "Premium", new Guid("22222222-2222-2222-2222-222222222222"), 1, "A" },
                    { new Guid("50000000-0000-0000-0000-000000000002"), "Premium", new Guid("22222222-2222-2222-2222-222222222222"), 2, "A" },
                    { new Guid("50000000-0000-0000-0000-000000000003"), "Premium", new Guid("22222222-2222-2222-2222-222222222222"), 3, "A" },
                    { new Guid("50000000-0000-0000-0000-000000000004"), "Premium", new Guid("22222222-2222-2222-2222-222222222222"), 4, "A" },
                    { new Guid("50000000-0000-0000-0000-000000000005"), "Premium", new Guid("22222222-2222-2222-2222-222222222222"), 5, "A" },
                    { new Guid("50000000-0000-0000-0000-000000000006"), "Premium", new Guid("22222222-2222-2222-2222-222222222222"), 6, "A" },
                    { new Guid("50000000-0000-0000-0000-000000000007"), "Standard", new Guid("22222222-2222-2222-2222-222222222222"), 1, "B" },
                    { new Guid("50000000-0000-0000-0000-000000000008"), "Standard", new Guid("22222222-2222-2222-2222-222222222222"), 2, "B" },
                    { new Guid("50000000-0000-0000-0000-000000000009"), "Standard", new Guid("22222222-2222-2222-2222-222222222222"), 3, "B" },
                    { new Guid("50000000-0000-0000-0000-000000000010"), "Standard", new Guid("22222222-2222-2222-2222-222222222222"), 4, "B" },
                    { new Guid("50000000-0000-0000-0000-000000000011"), "Standard", new Guid("22222222-2222-2222-2222-222222222222"), 5, "B" },
                    { new Guid("50000000-0000-0000-0000-000000000012"), "Standard", new Guid("22222222-2222-2222-2222-222222222222"), 6, "B" }
                });

            migrationBuilder.InsertData(
                table: "session_seats",
                columns: new[] { "Id", "EventSessionId", "Price", "ReservationId", "ReservedUntilUtc", "SeatId", "Status" },
                values: new object[,]
                {
                    { new Guid("60000000-0000-0000-0000-000000000001"), new Guid("44444444-4444-4444-4444-444444444444"), 80.00m, null, null, new Guid("50000000-0000-0000-0000-000000000001"), "Available" },
                    { new Guid("60000000-0000-0000-0000-000000000002"), new Guid("44444444-4444-4444-4444-444444444444"), 80.00m, null, null, new Guid("50000000-0000-0000-0000-000000000002"), "Available" },
                    { new Guid("60000000-0000-0000-0000-000000000003"), new Guid("44444444-4444-4444-4444-444444444444"), 80.00m, null, null, new Guid("50000000-0000-0000-0000-000000000003"), "Available" },
                    { new Guid("60000000-0000-0000-0000-000000000004"), new Guid("44444444-4444-4444-4444-444444444444"), 80.00m, null, null, new Guid("50000000-0000-0000-0000-000000000004"), "Available" },
                    { new Guid("60000000-0000-0000-0000-000000000005"), new Guid("44444444-4444-4444-4444-444444444444"), 80.00m, null, null, new Guid("50000000-0000-0000-0000-000000000005"), "Available" },
                    { new Guid("60000000-0000-0000-0000-000000000006"), new Guid("44444444-4444-4444-4444-444444444444"), 80.00m, null, null, new Guid("50000000-0000-0000-0000-000000000006"), "Available" },
                    { new Guid("60000000-0000-0000-0000-000000000007"), new Guid("44444444-4444-4444-4444-444444444444"), 50.00m, null, null, new Guid("50000000-0000-0000-0000-000000000007"), "Available" },
                    { new Guid("60000000-0000-0000-0000-000000000008"), new Guid("44444444-4444-4444-4444-444444444444"), 50.00m, null, null, new Guid("50000000-0000-0000-0000-000000000008"), "Available" },
                    { new Guid("60000000-0000-0000-0000-000000000009"), new Guid("44444444-4444-4444-4444-444444444444"), 50.00m, null, null, new Guid("50000000-0000-0000-0000-000000000009"), "Available" },
                    { new Guid("60000000-0000-0000-0000-000000000010"), new Guid("44444444-4444-4444-4444-444444444444"), 50.00m, null, null, new Guid("50000000-0000-0000-0000-000000000010"), "Available" },
                    { new Guid("60000000-0000-0000-0000-000000000011"), new Guid("44444444-4444-4444-4444-444444444444"), 50.00m, null, null, new Guid("50000000-0000-0000-0000-000000000011"), "Available" },
                    { new Guid("60000000-0000-0000-0000-000000000012"), new Guid("44444444-4444-4444-4444-444444444444"), 50.00m, null, null, new Guid("50000000-0000-0000-0000-000000000012"), "Available" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_event_sessions_EventId_StartsAtUtc",
                table: "event_sessions",
                columns: new[] { "EventId", "StartsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_event_sessions_HallId_StartsAtUtc",
                table: "event_sessions",
                columns: new[] { "HallId", "StartsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_events_Title",
                table: "events",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_halls_VenueId_Name",
                table: "halls",
                columns: new[] { "VenueId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_ExternalReference",
                table: "payments",
                column: "ExternalReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_ReservationId",
                table: "payments",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_reservation_seats_ReservationId_SessionSeatId",
                table: "reservation_seats",
                columns: new[] { "ReservationId", "SessionSeatId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_reservation_seats_SessionSeatId",
                table: "reservation_seats",
                column: "SessionSeatId");

            migrationBuilder.CreateIndex(
                name: "IX_reservations_EventSessionId",
                table: "reservations",
                column: "EventSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_reservations_Status_ExpiresAtUtc",
                table: "reservations",
                columns: new[] { "Status", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_reservations_UserId_CreatedAtUtc",
                table: "reservations",
                columns: new[] { "UserId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_seats_HallId_RowLabel_Number",
                table: "seats",
                columns: new[] { "HallId", "RowLabel", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_session_seats_EventSessionId_SeatId",
                table: "session_seats",
                columns: new[] { "EventSessionId", "SeatId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_session_seats_ReservationId",
                table: "session_seats",
                column: "ReservationId");

            migrationBuilder.CreateIndex(
                name: "IX_session_seats_SeatId",
                table: "session_seats",
                column: "SeatId");

            migrationBuilder.CreateIndex(
                name: "IX_session_seats_Status",
                table: "session_seats",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_venues_Name",
                table: "venues",
                column: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "reservation_seats");

            migrationBuilder.DropTable(
                name: "session_seats");

            migrationBuilder.DropTable(
                name: "reservations");

            migrationBuilder.DropTable(
                name: "seats");

            migrationBuilder.DropTable(
                name: "event_sessions");

            migrationBuilder.DropTable(
                name: "events");

            migrationBuilder.DropTable(
                name: "halls");

            migrationBuilder.DropTable(
                name: "venues");
        }
    }
}
