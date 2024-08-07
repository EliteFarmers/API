using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChocolateFortuneUpgrades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CocoaFortuneUpgrades",
                table: "ChocolateFactories",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RefinedTrufflesConsumed",
                table: "ChocolateFactories",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CocoaFortuneUpgrades",
                table: "ChocolateFactories");

            migrationBuilder.DropColumn(
                name: "RefinedTrufflesConsumed",
                table: "ChocolateFactories");
        }
    }
}
