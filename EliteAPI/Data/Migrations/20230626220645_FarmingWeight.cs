using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class FarmingWeight : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MinecraftAccounts_Accounts_AccountId",
                table: "MinecraftAccounts");

            migrationBuilder.RenameColumn(
                name: "LastSave",
                table: "Profiles",
                newName: "LastUpdated");

            migrationBuilder.RenameColumn(
                name: "AccountId",
                table: "MinecraftAccounts",
                newName: "AccountEntitiesId");

            migrationBuilder.RenameIndex(
                name: "IX_MinecraftAccounts_AccountId",
                table: "MinecraftAccounts",
                newName: "IX_MinecraftAccounts_AccountEntitiesId");

            migrationBuilder.CreateTable(
                name: "FarmingWeights",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TotalWeight = table.Column<double>(type: "double precision", nullable: false),
                    CropWeight = table.Column<Dictionary<string, double>>(type: "jsonb", nullable: false),
                    BonusWeight = table.Column<Dictionary<string, int>>(type: "jsonb", nullable: false),
                    ProfileMemberId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FarmingWeights", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FarmingWeights_ProfileMembers_ProfileMemberId",
                        column: x => x.ProfileMemberId,
                        principalTable: "ProfileMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FarmingWeights_ProfileMemberId",
                table: "FarmingWeights",
                column: "ProfileMemberId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MinecraftAccounts_Accounts_AccountEntitiesId",
                table: "MinecraftAccounts",
                column: "AccountEntitiesId",
                principalTable: "Accounts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MinecraftAccounts_Accounts_AccountEntitiesId",
                table: "MinecraftAccounts");

            migrationBuilder.DropTable(
                name: "FarmingWeights");

            migrationBuilder.RenameColumn(
                name: "LastUpdated",
                table: "Profiles",
                newName: "LastSave");

            migrationBuilder.RenameColumn(
                name: "AccountEntitiesId",
                table: "MinecraftAccounts",
                newName: "AccountId");

            migrationBuilder.RenameIndex(
                name: "IX_MinecraftAccounts_AccountEntitiesId",
                table: "MinecraftAccounts",
                newName: "IX_MinecraftAccounts_AccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_MinecraftAccounts_Accounts_AccountId",
                table: "MinecraftAccounts",
                column: "AccountId",
                principalTable: "Accounts",
                principalColumn: "Id");
        }
    }
}
