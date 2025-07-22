using System;
using System.Collections.Generic;
using EliteAPI.Features.Account.Models;
using EliteAPI.Models.Entities.Monetization;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscordMonetizationEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Inventory",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "Permissions",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "Purchases",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "Redemptions",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "Settings",
                table: "Accounts");

            migrationBuilder.AddColumn<bool>(
                name: "ActiveRewards",
                table: "Guilds",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ActiveRewards",
                table: "Accounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "UserSettingsId",
                table: "Accounts",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Icon = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Slug = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Flags = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Features = table.Column<UnlockedProductFeatures>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Features = table.Column<ConfiguredProductFeatures>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Entitlements",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Target = table.Column<int>(type: "integer", nullable: false),
                    ProductId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Deleted = table.Column<bool>(type: "boolean", nullable: false),
                    Consumed = table.Column<bool>(type: "boolean", nullable: true),
                    StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    AccountId = table.Column<decimal>(type: "numeric(20,0)", maxLength: 22, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Entitlements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Entitlements_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Entitlements_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Entitlements_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_UserSettingsId",
                table: "Accounts",
                column: "UserSettingsId");

            migrationBuilder.CreateIndex(
                name: "IX_Entitlements_AccountId",
                table: "Entitlements",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Entitlements_GuildId",
                table: "Entitlements",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Entitlements_ProductId",
                table: "Entitlements",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_UserSettings_UserSettingsId",
                table: "Accounts",
                column: "UserSettingsId",
                principalTable: "UserSettings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_UserSettings_UserSettingsId",
                table: "Accounts");

            migrationBuilder.DropTable(
                name: "Entitlements");

            migrationBuilder.DropTable(
                name: "UserSettings");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_UserSettingsId",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "ActiveRewards",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "ActiveRewards",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "UserSettingsId",
                table: "Accounts");

            migrationBuilder.AddColumn<EliteInventory>(
                name: "Inventory",
                table: "Accounts",
                type: "jsonb",
                nullable: false,
                defaultValue: new EliteInventory());

            migrationBuilder.AddColumn<int>(
                name: "Permissions",
                table: "Accounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<List<Purchase>>(
                name: "Purchases",
                table: "Accounts",
                type: "jsonb",
                nullable: false,
                defaultValue: new List<Purchase>());

            migrationBuilder.AddColumn<List<Redemption>>(
                name: "Redemptions",
                table: "Accounts",
                type: "jsonb",
                nullable: false,
                defaultValue: new List<Redemption>());

            migrationBuilder.AddColumn<EliteSettings>(
                name: "Settings",
                table: "Accounts",
                type: "jsonb",
                nullable: false,
                defaultValue: new EliteSettings());
        }
    }
}
