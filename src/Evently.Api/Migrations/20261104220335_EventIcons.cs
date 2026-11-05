using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Evently.Api.Migrations
{
    /// <inheritdoc />
    public partial class EventIcons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "IconUpdatedAt",
                table: "Events",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EventIcons",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "integer", nullable: false),
                    Data = table.Column<byte[]>(type: "bytea", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventIcons", x => x.EventId);
                    table.ForeignKey(
                        name: "FK_EventIcons_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventIcons");

            migrationBuilder.DropColumn(
                name: "IconUpdatedAt",
                table: "Events");
        }
    }
}
