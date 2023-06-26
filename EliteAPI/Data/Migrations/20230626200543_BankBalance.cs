using EliteAPI.Models.Entities.Hypixel;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class BankBalance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Banking",
                table: "Profiles");

            migrationBuilder.AddColumn<double>(
                name: "BankBalance",
                table: "Profiles",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankBalance",
                table: "Profiles");

            migrationBuilder.AddColumn<ProfileBanking>(
                name: "Banking",
                table: "Profiles",
                type: "jsonb",
                nullable: false);
        }
    }
}
