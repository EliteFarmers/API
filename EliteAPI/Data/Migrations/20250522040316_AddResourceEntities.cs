using System;
using EliteFarmers.HypixelAPI.DTOs;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BazaarProductSnapshots",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    InstaSellPrice = table.Column<double>(type: "double precision", nullable: false),
                    InstaBuyPrice = table.Column<double>(type: "double precision", nullable: false),
                    BuyOrderPrice = table.Column<double>(type: "double precision", nullable: false),
                    SellOrderPrice = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BazaarProductSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SkyblockItems",
                columns: table => new
                {
                    ItemId = table.Column<string>(type: "text", nullable: false),
                    NpcSellPrice = table.Column<double>(type: "double precision", nullable: false),
                    Data = table.Column<ItemResponse>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkyblockItems", x => x.ItemId);
                });

            migrationBuilder.CreateTable(
                name: "BazaarProductSummaries",
                columns: table => new
                {
                    ItemId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CalculationTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    InstaSellPrice = table.Column<double>(type: "double precision", nullable: false),
                    InstaBuyPrice = table.Column<double>(type: "double precision", nullable: false),
                    BuyOrderPrice = table.Column<double>(type: "double precision", nullable: false),
                    SellOrderPrice = table.Column<double>(type: "double precision", nullable: false),
                    AvgInstaSellPrice = table.Column<double>(type: "double precision", nullable: false),
                    AvgInstaBuyPrice = table.Column<double>(type: "double precision", nullable: false),
                    AvgBuyOrderPrice = table.Column<double>(type: "double precision", nullable: false),
                    AvgSellOrderPrice = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BazaarProductSummaries", x => x.ItemId);
                    table.ForeignKey(
                        name: "FK_BazaarProductSummaries_SkyblockItems_ItemId",
                        column: x => x.ItemId,
                        principalTable: "SkyblockItems",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BazaarProductSnapshots_ProductId",
                table: "BazaarProductSnapshots",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_BazaarProductSnapshots_ProductId_RecordedAt",
                table: "BazaarProductSnapshots",
                columns: new[] { "ProductId", "RecordedAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BazaarProductSnapshots_RecordedAt",
                table: "BazaarProductSnapshots",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BazaarProductSummaries_CalculationTimestamp",
                table: "BazaarProductSummaries",
                column: "CalculationTimestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BazaarProductSnapshots");

            migrationBuilder.DropTable(
                name: "BazaarProductSummaries");

            migrationBuilder.DropTable(
                name: "SkyblockItems");
        }
    }
}
