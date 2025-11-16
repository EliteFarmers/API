using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillLevelCaps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MinecraftAccounts_HypixelGuildMembers_GuildMemberId",
                table: "MinecraftAccounts");

            migrationBuilder.DropIndex(
                name: "IX_MinecraftAccounts_GuildMemberId",
                table: "MinecraftAccounts");

            migrationBuilder.DropColumn(
                name: "GuildMemberId",
                table: "MinecraftAccounts");

            migrationBuilder.AddColumn<Dictionary<string, int>>(
                name: "LevelCaps",
                table: "Skills",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LevelCaps",
                table: "Skills");

            migrationBuilder.AddColumn<long>(
                name: "GuildMemberId",
                table: "MinecraftAccounts",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MinecraftAccounts_GuildMemberId",
                table: "MinecraftAccounts",
                column: "GuildMemberId");

            migrationBuilder.AddForeignKey(
                name: "FK_MinecraftAccounts_HypixelGuildMembers_GuildMemberId",
                table: "MinecraftAccounts",
                column: "GuildMemberId",
                principalTable: "HypixelGuildMembers",
                principalColumn: "Id");
        }
    }
}
