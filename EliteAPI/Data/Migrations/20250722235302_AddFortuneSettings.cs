using EliteAPI.Features.Account.DTOs;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFortuneSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<FortuneSettingsDto>(
                name: "Fortune",
                table: "UserSettings",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NameStyleId",
                table: "UserSettings",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSettings_NameStyleId",
                table: "UserSettings",
                column: "NameStyleId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSettings_Cosmetics_NameStyleId",
                table: "UserSettings",
                column: "NameStyleId",
                principalTable: "Cosmetics",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSettings_Cosmetics_NameStyleId",
                table: "UserSettings");

            migrationBuilder.DropIndex(
                name: "IX_UserSettings_NameStyleId",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "Fortune",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "NameStyleId",
                table: "UserSettings");
        }
    }
}
