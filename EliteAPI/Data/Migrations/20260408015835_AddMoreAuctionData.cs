using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreAuctionData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EndedAuctions_Price",
                table: "EndedAuctions");

            migrationBuilder.DropIndex(
                name: "IX_AuctionBinPrices_AuctionUuid",
                table: "AuctionBinPrices");

            migrationBuilder.DropIndex(
                name: "IX_AuctionBinPrices_SkyblockId_VariantKey_ListedAt",
                table: "AuctionBinPrices");

            migrationBuilder.AddColumn<decimal>(
                name: "LastLowest",
                table: "AuctionItems",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastLowestAt",
                table: "AuctionItems",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RawLowest",
                table: "AuctionItems",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastLowest",
                table: "AuctionItems");

            migrationBuilder.DropColumn(
                name: "LastLowestAt",
                table: "AuctionItems");

            migrationBuilder.DropColumn(
                name: "RawLowest",
                table: "AuctionItems");

            migrationBuilder.CreateIndex(
                name: "IX_EndedAuctions_Price",
                table: "EndedAuctions",
                column: "Price");

            migrationBuilder.CreateIndex(
                name: "IX_AuctionBinPrices_AuctionUuid",
                table: "AuctionBinPrices",
                column: "AuctionUuid");

            migrationBuilder.CreateIndex(
                name: "IX_AuctionBinPrices_SkyblockId_VariantKey_ListedAt",
                table: "AuctionBinPrices",
                columns: new[] { "SkyblockId", "VariantKey", "ListedAt" });
        }
    }
}
