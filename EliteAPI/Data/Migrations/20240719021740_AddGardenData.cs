using System;
using System.Collections.Generic;
using EliteAPI.Models.Entities.Hypixel;
using HypixelAPI.DTOs;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGardenData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Gardens",
                columns: table => new
                {
                    ProfileId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    GardenExperience = table.Column<long>(type: "bigint", nullable: false),
                    CompletedVisitors = table.Column<int>(type: "integer", nullable: false),
                    UniqueVisitors = table.Column<int>(type: "integer", nullable: false),
                    Crops_Wheat = table.Column<long>(type: "bigint", nullable: false),
                    Crops_Carrot = table.Column<long>(type: "bigint", nullable: false),
                    Crops_Potato = table.Column<long>(type: "bigint", nullable: false),
                    Crops_Pumpkin = table.Column<long>(type: "bigint", nullable: false),
                    Crops_Melon = table.Column<long>(type: "bigint", nullable: false),
                    Crops_Mushroom = table.Column<long>(type: "bigint", nullable: false),
                    Crops_CocoaBeans = table.Column<long>(type: "bigint", nullable: false),
                    Crops_Cactus = table.Column<long>(type: "bigint", nullable: false),
                    Crops_SugarCane = table.Column<long>(type: "bigint", nullable: false),
                    Crops_NetherWart = table.Column<long>(type: "bigint", nullable: false),
                    Upgrades_Wheat = table.Column<short>(type: "smallint", nullable: false),
                    Upgrades_Carrot = table.Column<short>(type: "smallint", nullable: false),
                    Upgrades_Potato = table.Column<short>(type: "smallint", nullable: false),
                    Upgrades_Pumpkin = table.Column<short>(type: "smallint", nullable: false),
                    Upgrades_Melon = table.Column<short>(type: "smallint", nullable: false),
                    Upgrades_Mushroom = table.Column<short>(type: "smallint", nullable: false),
                    Upgrades_CocoaBeans = table.Column<short>(type: "smallint", nullable: false),
                    Upgrades_Cactus = table.Column<short>(type: "smallint", nullable: false),
                    Upgrades_SugarCane = table.Column<short>(type: "smallint", nullable: false),
                    Upgrades_NetherWart = table.Column<short>(type: "smallint", nullable: false),
                    UnlockedPlots = table.Column<long>(type: "bigint", nullable: false),
                    Composter = table.Column<ComposterData>(type: "jsonb", nullable: true),
                    Visitors = table.Column<Dictionary<string, VisitorData>>(type: "jsonb", nullable: false),
                    LastUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gardens", x => x.ProfileId);
                    table.ForeignKey(
                        name: "FK_Gardens_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "ProfileId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Gardens");
        }
    }
}
