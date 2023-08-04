using System.Collections.Generic;
using EliteAPI.Models.Entities;
using EliteAPI.Models.Entities.Farming;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFarmingInventories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Inventories_ProfileMemberId",
                table: "Inventories");

            migrationBuilder.AddColumn<List<FlagReason>>(
                name: "FlagReasons",
                table: "MinecraftAccounts",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Flags",
                table: "MinecraftAccounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<FarmingFortune>(
                name: "Fortune",
                table: "FarmingWeights",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<FarmingInventory>(
                name: "Inventory",
                table: "FarmingWeights",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_ProfileMemberId",
                table: "Inventories",
                column: "ProfileMemberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Inventories_ProfileMemberId",
                table: "Inventories");

            migrationBuilder.DropColumn(
                name: "FlagReasons",
                table: "MinecraftAccounts");

            migrationBuilder.DropColumn(
                name: "Flags",
                table: "MinecraftAccounts");

            migrationBuilder.DropColumn(
                name: "Fortune",
                table: "FarmingWeights");

            migrationBuilder.DropColumn(
                name: "Inventory",
                table: "FarmingWeights");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_ProfileMemberId",
                table: "Inventories",
                column: "ProfileMemberId",
                unique: true);
        }
    }
}
