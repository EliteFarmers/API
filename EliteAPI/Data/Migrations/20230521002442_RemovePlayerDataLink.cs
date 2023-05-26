using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemovePlayerDataLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProfileMembers_PlayerData_PlayerDataId",
                table: "ProfileMembers");

            migrationBuilder.DropIndex(
                name: "IX_ProfileMembers_PlayerDataId",
                table: "ProfileMembers");

            migrationBuilder.DropColumn(
                name: "PlayerDataId",
                table: "ProfileMembers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PlayerDataId",
                table: "ProfileMembers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ProfileMembers_PlayerDataId",
                table: "ProfileMembers",
                column: "PlayerDataId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProfileMembers_PlayerData_PlayerDataId",
                table: "ProfileMembers",
                column: "PlayerDataId",
                principalTable: "PlayerData",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
