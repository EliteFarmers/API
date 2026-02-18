using EliteAPI.Models.Entities.Hypixel;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGreenhouseData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Hunting",
                table: "Skills",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Hunting",
                table: "SkillExperiences",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<ProfileMemberData>(
                name: "MemberData",
                table: "ProfileMembers",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<GardenUpgradesData>(
                name: "GardenUpgrades",
                table: "Gardens",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.AddColumn<long>(
                name: "GreenhouseSlotsMaskHigh",
                table: "Gardens",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "GreenhouseSlotsMaskLow",
                table: "Gardens",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "LastGrowthStageTime",
                table: "Gardens",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Hunting",
                table: "Skills");

            migrationBuilder.DropColumn(
                name: "Hunting",
                table: "SkillExperiences");

            migrationBuilder.DropColumn(
                name: "MemberData",
                table: "ProfileMembers");

            migrationBuilder.DropColumn(
                name: "GardenUpgrades",
                table: "Gardens");

            migrationBuilder.DropColumn(
                name: "GreenhouseSlotsMaskHigh",
                table: "Gardens");

            migrationBuilder.DropColumn(
                name: "GreenhouseSlotsMaskLow",
                table: "Gardens");

            migrationBuilder.DropColumn(
                name: "LastGrowthStageTime",
                table: "Gardens");
        }
    }
}
