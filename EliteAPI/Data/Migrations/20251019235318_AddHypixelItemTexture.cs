using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHypixelItemTexture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_HypixelItems_Images_ImageId",
                table: "HypixelItems");

            migrationBuilder.DropIndex(
                name: "IX_HypixelItems_ImageId",
                table: "HypixelItems");

            migrationBuilder.DropColumn(
                name: "ImageId",
                table: "HypixelItems");

            migrationBuilder.CreateTable(
                name: "HypixelItemTextures",
                columns: table => new
                {
                    RenderHash = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    LastUsed = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HypixelItemTextures", x => x.RenderHash);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HypixelItemTextures");

            migrationBuilder.AddColumn<string>(
                name: "ImageId",
                table: "HypixelItems",
                type: "character varying(48)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_HypixelItems_ImageId",
                table: "HypixelItems",
                column: "ImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_HypixelItems_Images_ImageId",
                table: "HypixelItems",
                column: "ImageId",
                principalTable: "Images",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
