using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGuideConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ConcurrencyVersion",
                table: "GuideVersions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "AvgTopBuyOrderPrice",
                table: "BazaarProductSummaries",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "AvgTopSellOrderPrice",
                table: "BazaarProductSummaries",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TopBuyOrderPrice",
                table: "BazaarProductSummaries",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TopSellOrderPrice",
                table: "BazaarProductSummaries",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TopBuyOrderPrice",
                table: "BazaarProductSnapshots",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TopSellOrderPrice",
                table: "BazaarProductSnapshots",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConcurrencyVersion",
                table: "GuideVersions");

            migrationBuilder.DropColumn(
                name: "AvgTopBuyOrderPrice",
                table: "BazaarProductSummaries");

            migrationBuilder.DropColumn(
                name: "AvgTopSellOrderPrice",
                table: "BazaarProductSummaries");

            migrationBuilder.DropColumn(
                name: "TopBuyOrderPrice",
                table: "BazaarProductSummaries");

            migrationBuilder.DropColumn(
                name: "TopSellOrderPrice",
                table: "BazaarProductSummaries");

            migrationBuilder.DropColumn(
                name: "TopBuyOrderPrice",
                table: "BazaarProductSnapshots");

            migrationBuilder.DropColumn(
                name: "TopSellOrderPrice",
                table: "BazaarProductSnapshots");
        }
    }
}
