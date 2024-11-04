using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddImageTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ProductWeightStyles_Products_ProductId",
                table: "ProductWeightStyles");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductWeightStyles_WeightStyles_WeightStyleId",
                table: "ProductWeightStyles");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSettings_WeightStyles_WeightStyleId",
                table: "UserSettings");

            migrationBuilder.DropTable(
                name: "WeightStyleImages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_WeightStyles",
                table: "WeightStyles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductWeightStyles",
                table: "ProductWeightStyles");

            migrationBuilder.DropColumn(
                name: "Banner",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "Icon",
                table: "Guilds");

            migrationBuilder.RenameTable(
                name: "WeightStyles",
                newName: "Cosmetics");

            migrationBuilder.RenameTable(
                name: "ProductWeightStyles",
                newName: "ProductCosmetics");

            migrationBuilder.RenameIndex(
                name: "IX_ProductWeightStyles_WeightStyleId",
                table: "ProductCosmetics",
                newName: "IX_ProductCosmetics_WeightStyleId");

            migrationBuilder.RenameIndex(
                name: "IX_ProductWeightStyles_ProductId",
                table: "ProductCosmetics",
                newName: "IX_ProductCosmetics_ProductId");

            migrationBuilder.AddColumn<bool>(
                name: "Available",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Price",
                table: "Products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "BannerId",
                table: "Guilds",
                type: "character varying(48)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IconId",
                table: "Guilds",
                type: "character varying(48)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageId",
                table: "Cosmetics",
                type: "character varying(48)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Cosmetics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Cosmetics",
                table: "Cosmetics",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductCosmetics",
                table: "ProductCosmetics",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Images",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    Path = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Title = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: true),
                    Hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Metadata = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: false),
                    ProductId = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Images", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Images_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Guilds_BannerId",
                table: "Guilds",
                column: "BannerId");

            migrationBuilder.CreateIndex(
                name: "IX_Guilds_IconId",
                table: "Guilds",
                column: "IconId");

            migrationBuilder.CreateIndex(
                name: "IX_Cosmetics_ImageId",
                table: "Cosmetics",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_Cosmetics_Type",
                table: "Cosmetics",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Images_Path",
                table: "Images",
                column: "Path",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Images_ProductId",
                table: "Images",
                column: "ProductId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cosmetics_Images_ImageId",
                table: "Cosmetics",
                column: "ImageId",
                principalTable: "Images",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Guilds_Images_BannerId",
                table: "Guilds",
                column: "BannerId",
                principalTable: "Images",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Guilds_Images_IconId",
                table: "Guilds",
                column: "IconId",
                principalTable: "Images",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCosmetics_Cosmetics_WeightStyleId",
                table: "ProductCosmetics",
                column: "WeightStyleId",
                principalTable: "Cosmetics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductCosmetics_Products_ProductId",
                table: "ProductCosmetics",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSettings_Cosmetics_WeightStyleId",
                table: "UserSettings",
                column: "WeightStyleId",
                principalTable: "Cosmetics",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cosmetics_Images_ImageId",
                table: "Cosmetics");

            migrationBuilder.DropForeignKey(
                name: "FK_Guilds_Images_BannerId",
                table: "Guilds");

            migrationBuilder.DropForeignKey(
                name: "FK_Guilds_Images_IconId",
                table: "Guilds");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductCosmetics_Cosmetics_WeightStyleId",
                table: "ProductCosmetics");

            migrationBuilder.DropForeignKey(
                name: "FK_ProductCosmetics_Products_ProductId",
                table: "ProductCosmetics");

            migrationBuilder.DropForeignKey(
                name: "FK_UserSettings_Cosmetics_WeightStyleId",
                table: "UserSettings");

            migrationBuilder.DropTable(
                name: "Images");

            migrationBuilder.DropIndex(
                name: "IX_Guilds_BannerId",
                table: "Guilds");

            migrationBuilder.DropIndex(
                name: "IX_Guilds_IconId",
                table: "Guilds");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ProductCosmetics",
                table: "ProductCosmetics");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Cosmetics",
                table: "Cosmetics");

            migrationBuilder.DropIndex(
                name: "IX_Cosmetics_ImageId",
                table: "Cosmetics");

            migrationBuilder.DropIndex(
                name: "IX_Cosmetics_Type",
                table: "Cosmetics");

            migrationBuilder.DropColumn(
                name: "Available",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Price",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "BannerId",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "IconId",
                table: "Guilds");

            migrationBuilder.DropColumn(
                name: "ImageId",
                table: "Cosmetics");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Cosmetics");

            migrationBuilder.RenameTable(
                name: "ProductCosmetics",
                newName: "ProductWeightStyles");

            migrationBuilder.RenameTable(
                name: "Cosmetics",
                newName: "WeightStyles");

            migrationBuilder.RenameIndex(
                name: "IX_ProductCosmetics_WeightStyleId",
                table: "ProductWeightStyles",
                newName: "IX_ProductWeightStyles_WeightStyleId");

            migrationBuilder.RenameIndex(
                name: "IX_ProductCosmetics_ProductId",
                table: "ProductWeightStyles",
                newName: "IX_ProductWeightStyles_ProductId");

            migrationBuilder.AddColumn<string>(
                name: "Banner",
                table: "Guilds",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "Guilds",
                type: "text",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ProductWeightStyles",
                table: "ProductWeightStyles",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_WeightStyles",
                table: "WeightStyles",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "WeightStyleImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WeightStyleId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Url = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeightStyleImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WeightStyleImages_WeightStyles_WeightStyleId",
                        column: x => x.WeightStyleId,
                        principalTable: "WeightStyles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WeightStyleImages_WeightStyleId",
                table: "WeightStyleImages",
                column: "WeightStyleId");

            migrationBuilder.AddForeignKey(
                name: "FK_ProductWeightStyles_Products_ProductId",
                table: "ProductWeightStyles",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ProductWeightStyles_WeightStyles_WeightStyleId",
                table: "ProductWeightStyles",
                column: "WeightStyleId",
                principalTable: "WeightStyles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_UserSettings_WeightStyles_WeightStyleId",
                table: "UserSettings",
                column: "WeightStyleId",
                principalTable: "WeightStyles",
                principalColumn: "Id");
        }
    }
}
