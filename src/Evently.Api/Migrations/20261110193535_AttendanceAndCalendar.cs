using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evently.Api.Migrations
{
    /// <inheritdoc />
    public partial class AttendanceAndCalendar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EditedAt",
                table: "Comments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CalendarToken",
                table: "AspNetUsers",
                type: "character varying(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Attendance",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "integer", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendance", x => new { x.EventId, x.UserName });
                    table.ForeignKey(
                        name: "FK_Attendance_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_CalendarToken",
                table: "AspNetUsers",
                column: "CalendarToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attendance");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_CalendarToken",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "EditedAt",
                table: "Comments");

            migrationBuilder.DropColumn(
                name: "CalendarToken",
                table: "AspNetUsers");
        }
    }
}
