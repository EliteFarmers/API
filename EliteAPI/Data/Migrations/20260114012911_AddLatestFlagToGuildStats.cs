using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLatestFlagToGuildStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HypixelGuildStats_GuildId",
                table: "HypixelGuildStats");

            migrationBuilder.AddColumn<bool>(
                name: "IsLatest",
                table: "HypixelGuildStats",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_HypixelGuildStats_GuildId_IsLatest",
                table: "HypixelGuildStats",
                columns: new[] { "GuildId", "IsLatest" },
                filter: "\"IsLatest\" = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HypixelGuildStats_GuildId_IsLatest",
                table: "HypixelGuildStats");

            migrationBuilder.DropColumn(
                name: "IsLatest",
                table: "HypixelGuildStats");

            migrationBuilder.CreateIndex(
                name: "IX_HypixelGuildStats_GuildId",
                table: "HypixelGuildStats",
                column: "GuildId");
        }
    }
}
