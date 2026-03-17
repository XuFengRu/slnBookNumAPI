using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookNumAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddGoogleCalendarTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 只建立 GoogleCalendarTokens（不要建立其他既有表，避免衝突）
            migrationBuilder.CreateTable(
                name: "GoogleCalendarTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoogleCalendarTokens", x => x.Id);
                });

            // 一個 UserId 只留一筆 refresh token（正式版比較合理）
            migrationBuilder.CreateIndex(
                name: "IX_GoogleCalendarTokens_UserId",
                table: "GoogleCalendarTokens",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoogleCalendarTokens");
        }
    }
}