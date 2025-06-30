using System.Collections.Generic;
using EliteAPI.Models.Entities.Accounts;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMinecraftAccountFace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Properties",
                table: "MinecraftAccounts");

            migrationBuilder.AddColumn<byte[]>(
                name: "Face",
                table: "MinecraftAccounts",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Hat",
                table: "MinecraftAccounts",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextureId",
                table: "MinecraftAccounts",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Face",
                table: "MinecraftAccounts");

            migrationBuilder.DropColumn(
                name: "Hat",
                table: "MinecraftAccounts");

            migrationBuilder.DropColumn(
                name: "TextureId",
                table: "MinecraftAccounts");

            migrationBuilder.AddColumn<List<MinecraftAccountProperty>>(
                name: "Properties",
                table: "MinecraftAccounts",
                type: "jsonb",
                nullable: false);
        }
    }
}
