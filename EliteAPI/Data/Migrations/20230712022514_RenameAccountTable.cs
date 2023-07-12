using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameAccountTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MinecraftAccounts_Accounts_AccountEntitiesId",
                table: "MinecraftAccounts");

            migrationBuilder.RenameColumn(
                name: "AccountEntitiesId",
                table: "MinecraftAccounts",
                newName: "AccountEntityId");

            migrationBuilder.RenameIndex(
                name: "IX_MinecraftAccounts_AccountEntitiesId",
                table: "MinecraftAccounts",
                newName: "IX_MinecraftAccounts_AccountEntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_MinecraftAccounts_Accounts_AccountEntityId",
                table: "MinecraftAccounts",
                column: "AccountEntityId",
                principalTable: "Accounts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MinecraftAccounts_Accounts_AccountEntityId",
                table: "MinecraftAccounts");

            migrationBuilder.RenameColumn(
                name: "AccountEntityId",
                table: "MinecraftAccounts",
                newName: "AccountEntitiesId");

            migrationBuilder.RenameIndex(
                name: "IX_MinecraftAccounts_AccountEntityId",
                table: "MinecraftAccounts",
                newName: "IX_MinecraftAccounts_AccountEntitiesId");

            migrationBuilder.AddForeignKey(
                name: "FK_MinecraftAccounts_Accounts_AccountEntitiesId",
                table: "MinecraftAccounts",
                column: "AccountEntitiesId",
                principalTable: "Accounts",
                principalColumn: "Id");
        }
    }
}
