using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHypixelInventories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Confirmations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Confirmations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HypixelInventory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    HypixelInventoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ProfileMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    EmptySlots = table.Column<short[]>(type: "smallint[]", nullable: true),
                    Hash = table.Column<string>(type: "text", nullable: false),
                    Metadata = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HypixelInventory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HypixelInventory_ProfileMembers_ProfileMemberId",
                        column: x => x.ProfileMemberId,
                        principalTable: "ProfileMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserConfirmations",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ConfirmationId = table.Column<int>(type: "integer", nullable: false),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConfirmations", x => new { x.UserId, x.ConfirmationId });
                    table.ForeignKey(
                        name: "FK_UserConfirmations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserConfirmations_Confirmations_ConfirmationId",
                        column: x => x.ConfirmationId,
                        principalTable: "Confirmations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HypixelItems",
                columns: table => new
                {
                    HypixelItemId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Uuid = table.Column<Guid>(type: "uuid", nullable: true),
                    SkyblockId = table.Column<string>(type: "text", nullable: false),
                    Id = table.Column<short>(type: "smallint", nullable: false),
                    Damage = table.Column<short>(type: "smallint", nullable: false),
                    Count = table.Column<short>(type: "smallint", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: true),
                    Lore = table.Column<string>(type: "text", nullable: true),
                    Modifier = table.Column<string>(type: "text", nullable: true),
                    RarityUpgrades = table.Column<string>(type: "text", nullable: true),
                    Timestamp = table.Column<string>(type: "text", nullable: true),
                    DonatedMuseum = table.Column<string>(type: "text", nullable: true),
                    Enchantments = table.Column<Dictionary<string, int>>(type: "jsonb", nullable: true),
                    Attributes = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: true),
                    Gems = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: true),
                    ImageId = table.Column<string>(type: "character varying(48)", nullable: true),
                    Slot = table.Column<string>(type: "text", nullable: true),
                    LastUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    InventoryId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HypixelItems", x => x.HypixelItemId);
                    table.ForeignKey(
                        name: "FK_HypixelItems_HypixelInventory_InventoryId",
                        column: x => x.InventoryId,
                        principalTable: "HypixelInventory",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HypixelItems_Images_ImageId",
                        column: x => x.ImageId,
                        principalTable: "Images",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Images_Hash",
                table: "Images",
                column: "Hash");

            migrationBuilder.CreateIndex(
                name: "IX_HypixelInventory_HypixelInventoryId",
                table: "HypixelInventory",
                column: "HypixelInventoryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_HypixelInventory_ProfileMemberId",
                table: "HypixelInventory",
                column: "ProfileMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_HypixelItems_ImageId",
                table: "HypixelItems",
                column: "ImageId");

            migrationBuilder.CreateIndex(
                name: "IX_HypixelItems_InventoryId",
                table: "HypixelItems",
                column: "InventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_HypixelItems_SkyblockId",
                table: "HypixelItems",
                column: "SkyblockId");

            migrationBuilder.CreateIndex(
                name: "IX_UserConfirmations_ConfirmationId",
                table: "UserConfirmations",
                column: "ConfirmationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HypixelItems");

            migrationBuilder.DropTable(
                name: "UserConfirmations");

            migrationBuilder.DropTable(
                name: "HypixelInventory");

            migrationBuilder.DropTable(
                name: "Confirmations");

            migrationBuilder.DropIndex(
                name: "IX_Images_Hash",
                table: "Images");
        }
    }
}
