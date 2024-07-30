using EliteAPI.Models.Entities.Monetization;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDataDrivenWeightStyles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WeightStyleId",
                table: "UserSettings",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "WeightStyles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StyleFormatter = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Collection = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Data = table.Column<WeightStyleData>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeightStyles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductWeightStyles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    WeightStyleId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductWeightStyles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductWeightStyles_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductWeightStyles_WeightStyles_WeightStyleId",
                        column: x => x.WeightStyleId,
                        principalTable: "WeightStyles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WeightStyleImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WeightStyleId = table.Column<int>(type: "integer", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: true),
                    Title = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Order = table.Column<int>(type: "integer", nullable: false)
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
                name: "IX_UserSettings_WeightStyleId",
                table: "UserSettings",
                column: "WeightStyleId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductWeightStyles_ProductId",
                table: "ProductWeightStyles",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductWeightStyles_WeightStyleId",
                table: "ProductWeightStyles",
                column: "WeightStyleId");

            migrationBuilder.CreateIndex(
                name: "IX_WeightStyleImages_WeightStyleId",
                table: "WeightStyleImages",
                column: "WeightStyleId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserSettings_WeightStyles_WeightStyleId",
                table: "UserSettings",
                column: "WeightStyleId",
                principalTable: "WeightStyles",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserSettings_WeightStyles_WeightStyleId",
                table: "UserSettings");

            migrationBuilder.DropTable(
                name: "ProductWeightStyles");

            migrationBuilder.DropTable(
                name: "WeightStyleImages");

            migrationBuilder.DropTable(
                name: "WeightStyles");

            migrationBuilder.DropIndex(
                name: "IX_UserSettings_WeightStyleId",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "WeightStyleId",
                table: "UserSettings");
        }
    }
}
