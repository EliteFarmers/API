using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGuildCollectionsAndSkills : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Dictionary<string, long>>(
                name: "Collections",
                table: "HypixelGuildStats",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<Dictionary<string, long>>(
                name: "Skills",
                table: "HypixelGuildStats",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Collections",
                table: "HypixelGuildStats");

            migrationBuilder.DropColumn(
                name: "Skills",
                table: "HypixelGuildStats");
        }
    }
}
