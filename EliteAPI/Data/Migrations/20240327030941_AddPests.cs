using System.Collections.Generic;
using EliteAPI.Models.Entities.Hypixel;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Pests_Beetle",
                table: "FarmingWeights",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Pests_Cricket",
                table: "FarmingWeights",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Pests_Earthworm",
                table: "FarmingWeights",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Pests_Fly",
                table: "FarmingWeights",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Pests_Locust",
                table: "FarmingWeights",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Pests_Mite",
                table: "FarmingWeights",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Pests_Mosquito",
                table: "FarmingWeights",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Pests_Moth",
                table: "FarmingWeights",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Pests_Rat",
                table: "FarmingWeights",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Pests_Slug",
                table: "FarmingWeights",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Dictionary<Crop, long>>(
                name: "UncountedCrops",
                table: "FarmingWeights",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Pests_Beetle",
                table: "FarmingWeights");

            migrationBuilder.DropColumn(
                name: "Pests_Cricket",
                table: "FarmingWeights");

            migrationBuilder.DropColumn(
                name: "Pests_Earthworm",
                table: "FarmingWeights");

            migrationBuilder.DropColumn(
                name: "Pests_Fly",
                table: "FarmingWeights");

            migrationBuilder.DropColumn(
                name: "Pests_Locust",
                table: "FarmingWeights");

            migrationBuilder.DropColumn(
                name: "Pests_Mite",
                table: "FarmingWeights");

            migrationBuilder.DropColumn(
                name: "Pests_Mosquito",
                table: "FarmingWeights");

            migrationBuilder.DropColumn(
                name: "Pests_Moth",
                table: "FarmingWeights");

            migrationBuilder.DropColumn(
                name: "Pests_Rat",
                table: "FarmingWeights");

            migrationBuilder.DropColumn(
                name: "Pests_Slug",
                table: "FarmingWeights");

            migrationBuilder.DropColumn(
                name: "UncountedCrops",
                table: "FarmingWeights");
        }
    }
}
