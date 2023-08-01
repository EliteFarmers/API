using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class Inventories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Stats",
                table: "ProfileMembers",
                newName: "Unparsed");

            migrationBuilder.AddColumn<bool>(
                name: "Api_Collections",
                table: "ProfileMembers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Api_Inventories",
                table: "ProfileMembers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Api_Museum",
                table: "ProfileMembers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Api_Skills",
                table: "ProfileMembers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Api_Vault",
                table: "ProfileMembers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Inventories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Inventory = table.Column<string>(type: "text", nullable: true),
                    EnderChest = table.Column<string>(type: "text", nullable: true),
                    Armor = table.Column<string>(type: "text", nullable: true),
                    Wardrobe = table.Column<string>(type: "text", nullable: true),
                    Equipment = table.Column<string>(type: "text", nullable: true),
                    PersonalVault = table.Column<string>(type: "text", nullable: true),
                    TalismanBag = table.Column<string>(type: "text", nullable: true),
                    Backpacks = table.Column<List<string>>(type: "jsonb", nullable: true),
                    ProfileMemberId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inventories_ProfileMembers_ProfileMemberId",
                        column: x => x.ProfileMemberId,
                        principalTable: "ProfileMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_ProfileMemberId",
                table: "Inventories",
                column: "ProfileMemberId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Inventories");

            migrationBuilder.DropColumn(
                name: "Api_Collections",
                table: "ProfileMembers");

            migrationBuilder.DropColumn(
                name: "Api_Inventories",
                table: "ProfileMembers");

            migrationBuilder.DropColumn(
                name: "Api_Museum",
                table: "ProfileMembers");

            migrationBuilder.DropColumn(
                name: "Api_Skills",
                table: "ProfileMembers");

            migrationBuilder.DropColumn(
                name: "Api_Vault",
                table: "ProfileMembers");

            migrationBuilder.RenameColumn(
                name: "Unparsed",
                table: "ProfileMembers",
                newName: "Stats");
        }
    }
}
