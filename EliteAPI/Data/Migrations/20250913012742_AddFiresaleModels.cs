using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFiresaleModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "SocialXp",
                table: "Profiles",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "SkyblockFiresales",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StartsAt = table.Column<long>(type: "bigint", nullable: false),
                    EndsAt = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkyblockFiresales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SkyblockFiresaleItems",
                columns: table => new
                {
                    FiresaleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemId = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<int>(type: "integer", nullable: false),
                    StartsAt = table.Column<long>(type: "bigint", nullable: false),
                    EndsAt = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkyblockFiresaleItems", x => new { x.FiresaleId, x.ItemId });
                    table.ForeignKey(
                        name: "FK_SkyblockFiresaleItems_SkyblockFiresales_FiresaleId",
                        column: x => x.FiresaleId,
                        principalTable: "SkyblockFiresales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SkyblockFiresales_StartsAt",
                table: "SkyblockFiresales",
                column: "StartsAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SkyblockFiresaleItems");

            migrationBuilder.DropTable(
                name: "SkyblockFiresales");

            migrationBuilder.DropColumn(
                name: "SocialXp",
                table: "Profiles");
        }
    }
}
