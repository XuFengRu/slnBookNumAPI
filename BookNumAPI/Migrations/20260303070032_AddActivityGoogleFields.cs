using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookNumAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddActivityGoogleFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GoogleEventId",
                table: "Activity",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GoogleSyncedAt",
                table: "Activity",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoogleEventId",
                table: "Activity");

            migrationBuilder.DropColumn(
                name: "GoogleSyncedAt",
                table: "Activity");
        }
    }
}
