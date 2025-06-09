using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAuctionItemEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuctionBinPrices",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SkyblockId = table.Column<string>(type: "text", nullable: false),
                    VariantKey = table.Column<string>(type: "text", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    ListedAt = table.Column<long>(type: "bigint", nullable: false),
                    AuctionUuid = table.Column<Guid>(type: "uuid", nullable: false),
                    IngestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuctionBinPrices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuctionItems",
                columns: table => new
                {
                    SkyblockId = table.Column<string>(type: "text", nullable: false),
                    VariantKey = table.Column<string>(type: "text", nullable: false),
                    Lowest = table.Column<decimal>(type: "numeric", nullable: true),
                    LowestVolume = table.Column<int>(type: "integer", nullable: false),
                    LowestObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Lowest3Day = table.Column<decimal>(type: "numeric", nullable: true),
                    Lowest3DayVolume = table.Column<int>(type: "integer", nullable: false),
                    Lowest7Day = table.Column<decimal>(type: "numeric", nullable: true),
                    Lowest7DayVolume = table.Column<int>(type: "integer", nullable: false),
                    CalculatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuctionItems", x => new { x.SkyblockId, x.VariantKey });
                    table.ForeignKey(
                        name: "FK_AuctionItems_SkyblockItems_SkyblockId",
                        column: x => x.SkyblockId,
                        principalTable: "SkyblockItems",
                        principalColumn: "ItemId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuctionBinPrices_AuctionUuid",
                table: "AuctionBinPrices",
                column: "AuctionUuid");

            migrationBuilder.CreateIndex(
                name: "IX_AuctionBinPrices_IngestedAt",
                table: "AuctionBinPrices",
                column: "IngestedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AuctionBinPrices_SkyblockId_VariantKey_ListedAt",
                table: "AuctionBinPrices",
                columns: new[] { "SkyblockId", "VariantKey", "ListedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AuctionItems_CalculatedAt",
                table: "AuctionItems",
                column: "CalculatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuctionBinPrices");

            migrationBuilder.DropTable(
                name: "AuctionItems");
        }
    }
}
