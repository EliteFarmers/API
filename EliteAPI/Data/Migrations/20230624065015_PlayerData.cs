using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class PlayerData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MinecraftAccounts_PlayerData_PlayerDataId",
                table: "MinecraftAccounts");

            migrationBuilder.DropIndex(
                name: "IX_MinecraftAccounts_PlayerDataId",
                table: "MinecraftAccounts");

            migrationBuilder.DropColumn(
                name: "PlayerDataId",
                table: "MinecraftAccounts");

            migrationBuilder.RenameColumn(
                name: "SocialMedia",
                table: "PlayerData",
                newName: "SocialMedia_Youtube");

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "PlayerData",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FirstLogin",
                table: "PlayerData",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "Karma",
                table: "PlayerData",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "LastLogin",
                table: "PlayerData",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "LastLogout",
                table: "PlayerData",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "LastUpdated",
                table: "PlayerData",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "MonthlyRankColor",
                table: "PlayerData",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MostRecentMonthlyPackageRank",
                table: "PlayerData",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NetworkExp",
                table: "PlayerData",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RewardHighScore",
                table: "PlayerData",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RewardScore",
                table: "PlayerData",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "RewardStreak",
                table: "PlayerData",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SocialMedia_Discord",
                table: "PlayerData",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SocialMedia_Hypixel",
                table: "PlayerData",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TotalDailyRewards",
                table: "PlayerData",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalRewards",
                table: "PlayerData",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Uuid",
                table: "PlayerData",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_PlayerData_Uuid",
                table: "PlayerData",
                column: "Uuid",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_PlayerData_MinecraftAccounts_Uuid",
                table: "PlayerData",
                column: "Uuid",
                principalTable: "MinecraftAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PlayerData_MinecraftAccounts_Uuid",
                table: "PlayerData");

            migrationBuilder.DropIndex(
                name: "IX_PlayerData_Uuid",
                table: "PlayerData");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "PlayerData");

            migrationBuilder.DropColumn(
                name: "FirstLogin",
                table: "PlayerData");

            migrationBuilder.DropColumn(
                name: "Karma",
                table: "PlayerData");

            migrationBuilder.DropColumn(
                name: "LastLogin",
                table: "PlayerData");

            migrationBuilder.DropColumn(
                name: "LastLogout",
                table: "PlayerData");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "PlayerData");

            migrationBuilder.DropColumn(
                name: "MonthlyRankColor",
                table: "PlayerData");

            migrationBuilder.DropColumn(
                name: "MostRecentMonthlyPackageRank",
                table: "PlayerData");

            migrationBuilder.DropColumn(
                name: "NetworkExp",
                table: "PlayerData");

            migrationBuilder.DropColumn(
                name: "RewardHighScore",
                table: "PlayerData");

            migrationBuilder.DropColumn(
                name: "RewardScore",
                table: "PlayerData");

            migrationBuilder.DropColumn(
                name: "RewardStreak",
                table: "PlayerData");

            migrationBuilder.DropColumn(
                name: "SocialMedia_Discord",
                table: "PlayerData");

            migrationBuilder.DropColumn(
                name: "SocialMedia_Hypixel",
                table: "PlayerData");

            migrationBuilder.DropColumn(
                name: "TotalDailyRewards",
                table: "PlayerData");

            migrationBuilder.DropColumn(
                name: "TotalRewards",
                table: "PlayerData");

            migrationBuilder.DropColumn(
                name: "Uuid",
                table: "PlayerData");

            migrationBuilder.RenameColumn(
                name: "SocialMedia_Youtube",
                table: "PlayerData",
                newName: "SocialMedia");

            migrationBuilder.AddColumn<int>(
                name: "PlayerDataId",
                table: "MinecraftAccounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_MinecraftAccounts_PlayerDataId",
                table: "MinecraftAccounts",
                column: "PlayerDataId");

            migrationBuilder.AddForeignKey(
                name: "FK_MinecraftAccounts_PlayerData_PlayerDataId",
                table: "MinecraftAccounts",
                column: "PlayerDataId",
                principalTable: "PlayerData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
