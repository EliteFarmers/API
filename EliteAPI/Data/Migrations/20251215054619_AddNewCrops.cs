using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNewCrops : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Deleted",
                table: "HypixelGuilds",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "Crops_Moonflower",
                table: "Gardens",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Crops_Sunflower",
                table: "Gardens",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Crops_WildRose",
                table: "Gardens",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<short>(
                name: "Upgrades_Moonflower",
                table: "Gardens",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "Upgrades_Sunflower",
                table: "Gardens",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "Upgrades_WildRose",
                table: "Gardens",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<int>(
                name: "Pests_Dragonfly",
                table: "FarmingWeights",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Pests_Firefly",
                table: "FarmingWeights",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Pests_Mantis",
                table: "FarmingWeights",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Dragonfly",
                table: "CropCollections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Firefly",
                table: "CropCollections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Mantis",
                table: "CropCollections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "Moonflower",
                table: "CropCollections",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Sunflower",
                table: "CropCollections",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "WildRose",
                table: "CropCollections",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Deleted",
                table: "HypixelGuilds");

            migrationBuilder.DropColumn(
                name: "Crops_Moonflower",
                table: "Gardens");

            migrationBuilder.DropColumn(
                name: "Crops_Sunflower",
                table: "Gardens");

            migrationBuilder.DropColumn(
                name: "Crops_WildRose",
                table: "Gardens");

            migrationBuilder.DropColumn(
                name: "Upgrades_Moonflower",
                table: "Gardens");

            migrationBuilder.DropColumn(
                name: "Upgrades_Sunflower",
                table: "Gardens");

            migrationBuilder.DropColumn(
                name: "Upgrades_WildRose",
                table: "Gardens");

            migrationBuilder.DropColumn(
                name: "Pests_Dragonfly",
                table: "FarmingWeights");

            migrationBuilder.DropColumn(
                name: "Pests_Firefly",
                table: "FarmingWeights");

            migrationBuilder.DropColumn(
                name: "Pests_Mantis",
                table: "FarmingWeights");

            migrationBuilder.DropColumn(
                name: "Dragonfly",
                table: "CropCollections");

            migrationBuilder.DropColumn(
                name: "Firefly",
                table: "CropCollections");

            migrationBuilder.DropColumn(
                name: "Mantis",
                table: "CropCollections");

            migrationBuilder.DropColumn(
                name: "Moonflower",
                table: "CropCollections");

            migrationBuilder.DropColumn(
                name: "Sunflower",
                table: "CropCollections");

            migrationBuilder.DropColumn(
                name: "WildRose",
                table: "CropCollections");
        }
    }
}
