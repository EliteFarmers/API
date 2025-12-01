using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNetworth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "MuseumLastUpdated",
                table: "Profiles",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<double>(
                name: "FunctionalNetworth",
                table: "ProfileMembers",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "LiquidFunctionalNetworth",
                table: "ProfileMembers",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "LiquidNetworth",
                table: "ProfileMembers",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Networth",
                table: "ProfileMembers",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "PersonalBank",
                table: "ProfileMembers",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MuseumLastUpdated",
                table: "Profiles");

            migrationBuilder.DropColumn(
                name: "FunctionalNetworth",
                table: "ProfileMembers");

            migrationBuilder.DropColumn(
                name: "LiquidFunctionalNetworth",
                table: "ProfileMembers");

            migrationBuilder.DropColumn(
                name: "LiquidNetworth",
                table: "ProfileMembers");

            migrationBuilder.DropColumn(
                name: "Networth",
                table: "ProfileMembers");

            migrationBuilder.DropColumn(
                name: "PersonalBank",
                table: "ProfileMembers");
        }
    }
}
