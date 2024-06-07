using System.Collections.Generic;
using EliteAPI.Models.Entities.Accounts;
using EliteAPI.Models.Entities.Discord;
using EliteAPI.Models.Entities.Events;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MinecraftAccounts_Accounts_AccountEntityId",
                table: "MinecraftAccounts");

            migrationBuilder.DropIndex(
                name: "IX_MinecraftAccounts_AccountEntityId",
                table: "MinecraftAccounts");

            migrationBuilder.DropColumn(
                name: "AccountEntityId",
                table: "MinecraftAccounts");

            migrationBuilder.DropColumn(
                name: "BlockedUsers",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Active",
                table: "EventMembers");

            migrationBuilder.DropColumn(
                name: "Collected",
                table: "EventMembers");

            migrationBuilder.DropColumn(
                name: "Disqualified",
                table: "EventMembers");

            migrationBuilder.RenameColumn(
                name: "Reason",
                table: "EventMembers",
                newName: "Notes");

            migrationBuilder.AddColumn<List<Badge>>(
                name: "Badges",
                table: "MinecraftAccounts",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BlockedRole",
                table: "Events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Category",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Public",
                table: "Events",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "RequiredRole",
                table: "Events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Target",
                table: "Events",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AmountGained",
                table: "EventMembers",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "EventMembers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_MinecraftAccounts_AccountId",
                table: "MinecraftAccounts",
                column: "AccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_MinecraftAccounts_Accounts_AccountId",
                table: "MinecraftAccounts",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MinecraftAccounts_Accounts_AccountId",
                table: "MinecraftAccounts");

            migrationBuilder.DropIndex(
                name: "IX_MinecraftAccounts_AccountId",
                table: "MinecraftAccounts");

            migrationBuilder.DropColumn(
                name: "Badges",
                table: "MinecraftAccounts");

            migrationBuilder.DropColumn(
                name: "BlockedRole",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Public",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "RequiredRole",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Target",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "AmountGained",
                table: "EventMembers");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "EventMembers");

            migrationBuilder.RenameColumn(
                name: "Notes",
                table: "EventMembers",
                newName: "Reason");

            migrationBuilder.AddColumn<decimal>(
                name: "AccountEntityId",
                table: "MinecraftAccounts",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AddColumn<List<BlockedUser>>(
                name: "BlockedUsers",
                table: "Events",
                type: "jsonb",
                nullable: false);

            migrationBuilder.AddColumn<bool>(
                name: "Active",
                table: "EventMembers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "Collected",
                table: "EventMembers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<bool>(
                name: "Disqualified",
                table: "EventMembers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_MinecraftAccounts_AccountEntityId",
                table: "MinecraftAccounts",
                column: "AccountEntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_MinecraftAccounts_Accounts_AccountEntityId",
                table: "MinecraftAccounts",
                column: "AccountEntityId",
                principalTable: "Accounts",
                principalColumn: "Id");
        }
    }
}
