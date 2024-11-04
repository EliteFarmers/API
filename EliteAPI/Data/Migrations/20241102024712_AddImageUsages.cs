using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddImageUsages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Images_Products_ProductId",
                table: "Images");

            migrationBuilder.DropIndex(
                name: "IX_Images_Path",
                table: "Images");

            migrationBuilder.DropIndex(
                name: "IX_Images_ProductId",
                table: "Images");

            migrationBuilder.DropIndex(
                name: "IX_EventTeams_EventId",
                table: "EventTeams");

            migrationBuilder.DropIndex(
                name: "IX_EventMembers_EventId",
                table: "EventMembers");

            migrationBuilder.DropColumn(
                name: "ProductId",
                table: "Images");

            migrationBuilder.DropColumn(
                name: "Banner",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Thumbnail",
                table: "Events");

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailId",
                table: "Products",
                type: "character varying(48)",
                maxLength: 48,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Guilds",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "InviteCode",
                table: "Guilds",
                type: "character varying(64)",
                maxLength: 16,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Approved",
                table: "Events",
                type: "boolean",
                nullable: false,
                defaultValue: false);
            
            // Set existing events to approved
            migrationBuilder.Sql("""UPDATE "Events" SET "Approved" = true""");

            migrationBuilder.AddColumn<string>(
                name: "BannerId",
                table: "Events",
                type: "character varying(48)",
                maxLength: 48,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ImageId",
                table: "Badges",
                type: "character varying(48)",
                maxLength: 48,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256);

            migrationBuilder.CreateTable(
                name: "ProductImage",
                columns: table => new
                {
                    ProductId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ImageId = table.Column<string>(type: "character varying(48)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImage", x => new { x.ProductId, x.ImageId });
                    table.ForeignKey(
                        name: "FK_ProductImage_Images_ImageId",
                        column: x => x.ImageId,
                        principalTable: "Images",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductImage_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_ThumbnailId",
                table: "Products",
                column: "ThumbnailId");

            migrationBuilder.CreateIndex(
                name: "IX_Images_Path",
                table: "Images",
                column: "Path");

            migrationBuilder.CreateIndex(
                name: "IX_EventTeams_EventId_UserId",
                table: "EventTeams",
                columns: new[] { "EventId", "UserId" },
                unique: true);
            
            migrationBuilder.CreateIndex(
                name: "IX_Events_BannerId",
                table: "Events",
                column: "BannerId");

            migrationBuilder.CreateIndex(
                name: "IX_EventMembers_EventId_UserId",
                table: "EventMembers",
                columns: new[] { "EventId", "UserId" },
                unique: true);
            
            // Clear ImageId column
            migrationBuilder.Sql("""UPDATE "Badges" SET "ImageId" = NULL""");

            migrationBuilder.CreateIndex(
                name: "IX_Badges_ImageId",
                table: "Badges",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImage_ImageId",
                table: "ProductImage",
                column: "ImageId");

            migrationBuilder.AddForeignKey(
                name: "FK_Badges_Images_ImageId",
                table: "Badges",
                column: "ImageId",
                principalTable: "Images",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Events_Images_BannerId",
                table: "Events",
                column: "BannerId",
                principalTable: "Images",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Images_ThumbnailId",
                table: "Products",
                column: "ThumbnailId",
                principalTable: "Images",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Badges_Images_ImageId",
                table: "Badges");

            migrationBuilder.DropForeignKey(
                name: "FK_Events_Images_BannerId",
                table: "Events");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Images_ThumbnailId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "ProductImage");

            migrationBuilder.DropIndex(
                name: "IX_Products_ThumbnailId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Images_Path",
                table: "Images");

            migrationBuilder.DropIndex(
                name: "IX_EventTeams_EventId_UserId",
                table: "EventTeams");

            migrationBuilder.DropIndex(
                name: "IX_Events_BannerId",
                table: "Events");

            migrationBuilder.DropIndex(
                name: "IX_EventMembers_EventId_UserId",
                table: "EventMembers");

            migrationBuilder.DropIndex(
                name: "IX_Badges_ImageId",
                table: "Badges");

            migrationBuilder.DropColumn(
                name: "ThumbnailId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Approved",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "BannerId",
                table: "Events");

            migrationBuilder.AddColumn<decimal>(
                name: "ProductId",
                table: "Images",
                type: "numeric(20,0)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Guilds",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(128)",
                oldMaxLength: 128);

            migrationBuilder.AlterColumn<string>(
                name: "InviteCode",
                table: "Guilds",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(16)",
                oldMaxLength: 16,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Banner",
                table: "Events",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Thumbnail",
                table: "Events",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ImageId",
                table: "Badges",
                type: "character varying(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(48)",
                oldMaxLength: 48,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Images_Path",
                table: "Images",
                column: "Path",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Images_ProductId",
                table: "Images",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTeams_EventId",
                table: "EventTeams",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventMembers_EventId",
                table: "EventMembers",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_Images_Products_ProductId",
                table: "Images",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id");
        }
    }
}
