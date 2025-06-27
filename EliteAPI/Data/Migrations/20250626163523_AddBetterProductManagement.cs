using System;
using EliteAPI.Features.Leaderboards.Models;
using EliteAPI.Models.Entities.Monetization;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBetterProductManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "Accounts");

            migrationBuilder.AddColumn<MemberLeaderboardCosmeticsDto>(
                name: "CustomLeaderboardStyle",
                table: "UserSettings",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmojiUrl",
                table: "UserSettings",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LeaderboardStyleId",
                table: "UserSettings",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Prefix",
                table: "UserSettings",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Suffix",
                table: "UserSettings",
                type: "character varying(16)",
                maxLength: 16,
                nullable: true);

            migrationBuilder.AddColumn<LeaderboardStyleData>(
                name: "Leaderboard",
                table: "Cosmetics",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<NameStyleData>(
                name: "NameStyle",
                table: "Cosmetics",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ShopOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BuyerId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    RecipientId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    RecipientGuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OrderDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    ProviderTransactionId = table.Column<string>(type: "text", nullable: true),
                    TotalPrice = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopOrders_Accounts_BuyerId",
                        column: x => x.BuyerId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShopOrders_Accounts_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ShopOrders_Guilds_RecipientGuildId",
                        column: x => x.RecipientGuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProductAccesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: true),
                    SourceOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Revoked = table.Column<bool>(type: "boolean", nullable: false),
                    Consumed = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductAccesses_Accounts_UserId",
                        column: x => x.UserId,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductAccesses_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ProductAccesses_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductAccesses_ShopOrders_SourceOrderId",
                        column: x => x.SourceOrderId,
                        principalTable: "ShopOrders",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ShopOrderItems",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShopOrderItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShopOrderItems_ShopOrders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "ShopOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSettings_LeaderboardStyleId",
                table: "UserSettings",
                column: "LeaderboardStyleId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAccesses_GuildId",
                table: "ProductAccesses",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAccesses_ProductId",
                table: "ProductAccesses",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAccesses_SourceOrderId",
                table: "ProductAccesses",
                column: "SourceOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductAccesses_UserId",
                table: "ProductAccesses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopOrderItems_OrderId",
                table: "ShopOrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopOrderItems_ProductId",
                table: "ShopOrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopOrders_BuyerId",
                table: "ShopOrders",
                column: "BuyerId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopOrders_RecipientGuildId",
                table: "ShopOrders",
                column: "RecipientGuildId");

            migrationBuilder.CreateIndex(
                name: "IX_ShopOrders_RecipientId",
                table: "ShopOrders",
                column: "RecipientId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSettings_Cosmetics_LeaderboardStyleId",
                table: "UserSettings",
                column: "LeaderboardStyleId",
                principalTable: "Cosmetics",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSettings_Cosmetics_LeaderboardStyleId",
                table: "UserSettings");

            migrationBuilder.DropTable(
                name: "ProductAccesses");

            migrationBuilder.DropTable(
                name: "ShopOrderItems");

            migrationBuilder.DropTable(
                name: "ShopOrders");

            migrationBuilder.DropIndex(
                name: "IX_UserSettings_LeaderboardStyleId",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "CustomLeaderboardStyle",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "EmojiUrl",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "LeaderboardStyleId",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "Prefix",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "Suffix",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "Leaderboard",
                table: "Cosmetics");

            migrationBuilder.DropColumn(
                name: "NameStyle",
                table: "Cosmetics");

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "Accounts",
                type: "text",
                nullable: true);
        }
    }
}
