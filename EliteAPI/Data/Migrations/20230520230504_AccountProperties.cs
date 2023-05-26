using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AccountProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Properties",
                table: "MinecraftAccounts");

            migrationBuilder.CreateTable(
                name: "MinecraftAccountProperty",
                columns: table => new
                {
                    MinecraftAccountId = table.Column<int>(type: "integer", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MinecraftAccountProperty", x => new { x.MinecraftAccountId, x.Id });
                    table.ForeignKey(
                        name: "FK_MinecraftAccountProperty_MinecraftAccounts_MinecraftAccount~",
                        column: x => x.MinecraftAccountId,
                        principalTable: "MinecraftAccounts",
                        principalColumn: "MinecraftAccountId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MinecraftAccountProperty");

            migrationBuilder.AddColumn<string>(
                name: "Properties",
                table: "MinecraftAccounts",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
