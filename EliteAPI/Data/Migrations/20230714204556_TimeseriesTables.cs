using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class TimeseriesTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CropCollections",
                columns: table => new
                {
                    Wheat = table.Column<long>(type: "bigint", nullable: false),
                    Carrot = table.Column<long>(type: "bigint", nullable: false),
                    Potato = table.Column<long>(type: "bigint", nullable: false),
                    Pumpkin = table.Column<long>(type: "bigint", nullable: false),
                    Melon = table.Column<long>(type: "bigint", nullable: false),
                    Mushroom = table.Column<long>(type: "bigint", nullable: false),
                    CocoaBeans = table.Column<long>(type: "bigint", nullable: false),
                    Cactus = table.Column<long>(type: "bigint", nullable: false),
                    SugarCane = table.Column<long>(type: "bigint", nullable: false),
                    NetherWart = table.Column<long>(type: "bigint", nullable: false),
                    Seeds = table.Column<long>(type: "bigint", nullable: false),
                    Time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProfileMemberId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_CropCollections_ProfileMembers_ProfileMemberId",
                        column: x => x.ProfileMemberId,
                        principalTable: "ProfileMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SkillExperiences",
                columns: table => new
                {
                    Combat = table.Column<double>(type: "double precision", nullable: false),
                    Mining = table.Column<double>(type: "double precision", nullable: false),
                    Foraging = table.Column<double>(type: "double precision", nullable: false),
                    Fishing = table.Column<double>(type: "double precision", nullable: false),
                    Enchanting = table.Column<double>(type: "double precision", nullable: false),
                    Alchemy = table.Column<double>(type: "double precision", nullable: false),
                    Carpentry = table.Column<double>(type: "double precision", nullable: false),
                    Runecrafting = table.Column<double>(type: "double precision", nullable: false),
                    Social = table.Column<double>(type: "double precision", nullable: false),
                    Taming = table.Column<double>(type: "double precision", nullable: false),
                    Farming = table.Column<double>(type: "double precision", nullable: false),
                    Time = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProfileMemberId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.ForeignKey(
                        name: "FK_SkillExperiences_ProfileMembers_ProfileMemberId",
                        column: x => x.ProfileMemberId,
                        principalTable: "ProfileMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CropCollections_ProfileMemberId",
                table: "CropCollections",
                column: "ProfileMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_SkillExperiences_ProfileMemberId",
                table: "SkillExperiences",
                column: "ProfileMemberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CropCollections");

            migrationBuilder.DropTable(
                name: "SkillExperiences");
        }
    }
}
