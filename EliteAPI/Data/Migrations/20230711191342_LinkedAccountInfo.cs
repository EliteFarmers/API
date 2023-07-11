using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class LinkedAccountInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreviousNames",
                table: "MinecraftAccounts");

            migrationBuilder.AddColumn<decimal>(
                name: "AccountId",
                table: "MinecraftAccounts",
                type: "numeric(20,0)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccountId",
                table: "MinecraftAccounts");

            migrationBuilder.AddColumn<Dictionary<string, long>>(
                name: "PreviousNames",
                table: "MinecraftAccounts",
                type: "jsonb",
                nullable: false);
        }
    }
}
