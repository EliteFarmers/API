using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMedalBracketColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "AccountId",
                table: "MinecraftAccounts",
                type: "numeric(20,0)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Bronze",
                table: "JacobContests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Diamond",
                table: "JacobContests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Gold",
                table: "JacobContests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Platinum",
                table: "JacobContests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Silver",
                table: "JacobContests",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "BotPermissions",
                table: "Guilds",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20)");

            migrationBuilder.AlterColumn<decimal>(
                name: "AdminRole",
                table: "Guilds",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Id",
                table: "Guilds",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20)");

            migrationBuilder.AlterColumn<decimal>(
                name: "OwnerId",
                table: "Events",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20)");

            migrationBuilder.AlterColumn<decimal>(
                name: "GuildId",
                table: "Events",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Id",
                table: "Events",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20)");

            migrationBuilder.AlterColumn<decimal>(
                name: "UserId",
                table: "EventMembers",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20)");

            migrationBuilder.AlterColumn<decimal>(
                name: "EventId",
                table: "EventMembers",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Id",
                table: "Accounts",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bronze",
                table: "JacobContests");

            migrationBuilder.DropColumn(
                name: "Diamond",
                table: "JacobContests");

            migrationBuilder.DropColumn(
                name: "Gold",
                table: "JacobContests");

            migrationBuilder.DropColumn(
                name: "Platinum",
                table: "JacobContests");

            migrationBuilder.DropColumn(
                name: "Silver",
                table: "JacobContests");

            migrationBuilder.AlterColumn<decimal>(
                name: "AccountId",
                table: "MinecraftAccounts",
                type: "numeric(20)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "BotPermissions",
                table: "Guilds",
                type: "numeric(20)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "AdminRole",
                table: "Guilds",
                type: "numeric(20)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Id",
                table: "Guilds",
                type: "numeric(20)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "OwnerId",
                table: "Events",
                type: "numeric(20)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "GuildId",
                table: "Events",
                type: "numeric(20)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Id",
                table: "Events",
                type: "numeric(20)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "UserId",
                table: "EventMembers",
                type: "numeric(20)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "EventId",
                table: "EventMembers",
                type: "numeric(20)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AlterColumn<decimal>(
                name: "Id",
                table: "Accounts",
                type: "numeric(20)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");
        }
    }
}
