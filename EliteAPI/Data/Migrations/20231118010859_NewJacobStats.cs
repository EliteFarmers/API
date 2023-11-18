using EliteAPI.Models.Entities.Hypixel;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class NewJacobStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FirstPlaceScores",
                table: "JacobData",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<JacobStats>(
                name: "Stats",
                table: "JacobData",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstPlaceScores",
                table: "JacobData");

            migrationBuilder.DropColumn(
                name: "Stats",
                table: "JacobData");
        }
    }
}
