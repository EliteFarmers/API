using System;
using EliteAPI.Features.Profiles.Models;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSlayersAndGuildStats : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_HypixelGuildMembers_GuildId",
                table: "HypixelGuildMembers");

            migrationBuilder.AddColumn<Slayers>(
                name: "Slayers",
                table: "ProfileMembers",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MemberCount",
                table: "HypixelGuilds",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "LeftAt",
                table: "HypixelGuildMembers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "GameModeHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfileId = table.Column<string>(type: "text", nullable: false),
                    Old = table.Column<string>(type: "text", nullable: false),
                    New = table.Column<string>(type: "text", nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameModeHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GameModeHistories_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "ProfileId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HypixelGuildStats",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuildId = table.Column<string>(type: "text", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    MemberCount = table.Column<int>(type: "integer", nullable: false),
                    HypixelLevel_Total = table.Column<double>(type: "double precision", nullable: false),
                    HypixelLevel_Average = table.Column<double>(type: "double precision", nullable: false),
                    SkyblockExperience_Total = table.Column<double>(type: "double precision", nullable: false),
                    SkyblockExperience_Average = table.Column<double>(type: "double precision", nullable: false),
                    SkillLevel_Total = table.Column<double>(type: "double precision", nullable: false),
                    SkillLevel_Average = table.Column<double>(type: "double precision", nullable: false),
                    SlayerExperience_Total = table.Column<double>(type: "double precision", nullable: false),
                    SlayerExperience_Average = table.Column<double>(type: "double precision", nullable: false),
                    CatacombsExperience_Total = table.Column<double>(type: "double precision", nullable: false),
                    CatacombsExperience_Average = table.Column<double>(type: "double precision", nullable: false),
                    FarmingWeight_Total = table.Column<double>(type: "double precision", nullable: false),
                    FarmingWeight_Average = table.Column<double>(type: "double precision", nullable: false),
                    Networth_Total = table.Column<double>(type: "double precision", nullable: false),
                    Networth_Average = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HypixelGuildStats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HypixelGuildStats_HypixelGuilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "HypixelGuilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HypixelGuilds_MemberCount",
                table: "HypixelGuilds",
                column: "MemberCount");

            migrationBuilder.CreateIndex(
                name: "IX_HypixelGuildMembers_Active",
                table: "HypixelGuildMembers",
                column: "Active");

            migrationBuilder.CreateIndex(
                name: "IX_HypixelGuildMembers_GuildId_Active",
                table: "HypixelGuildMembers",
                columns: new[] { "GuildId", "Active" });
            
            migrationBuilder.Sql(@"
                DELETE FROM ""HypixelGuildMembers""
                WHERE ""Id"" NOT IN (
                    SELECT MIN(""Id"")
                    FROM ""HypixelGuildMembers""
                    GROUP BY ""GuildId"", ""PlayerUuid""
                );
            ");
            
            migrationBuilder.CreateIndex(
                name: "IX_HypixelGuildMembers_GuildId_PlayerUuid",
                table: "HypixelGuildMembers",
                columns: new[] { "GuildId", "PlayerUuid" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GameModeHistories_ProfileId",
                table: "GameModeHistories",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_HypixelGuildStats_CatacombsExperience_Total",
                table: "HypixelGuildStats",
                column: "CatacombsExperience_Total");

            migrationBuilder.CreateIndex(
                name: "IX_HypixelGuildStats_FarmingWeight_Total",
                table: "HypixelGuildStats",
                column: "FarmingWeight_Total");

            migrationBuilder.CreateIndex(
                name: "IX_HypixelGuildStats_GuildId",
                table: "HypixelGuildStats",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_HypixelGuildStats_HypixelLevel_Total",
                table: "HypixelGuildStats",
                column: "HypixelLevel_Total");

            migrationBuilder.CreateIndex(
                name: "IX_HypixelGuildStats_Networth_Average",
                table: "HypixelGuildStats",
                column: "Networth_Average");

            migrationBuilder.CreateIndex(
                name: "IX_HypixelGuildStats_Networth_Total",
                table: "HypixelGuildStats",
                column: "Networth_Total");

            migrationBuilder.CreateIndex(
                name: "IX_HypixelGuildStats_RecordedAt",
                table: "HypixelGuildStats",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_HypixelGuildStats_SkillLevel_Average",
                table: "HypixelGuildStats",
                column: "SkillLevel_Average");

            migrationBuilder.CreateIndex(
                name: "IX_HypixelGuildStats_SkillLevel_Total",
                table: "HypixelGuildStats",
                column: "SkillLevel_Total");

            migrationBuilder.CreateIndex(
                name: "IX_HypixelGuildStats_SkyblockExperience_Average",
                table: "HypixelGuildStats",
                column: "SkyblockExperience_Average");

            migrationBuilder.CreateIndex(
                name: "IX_HypixelGuildStats_SkyblockExperience_Total",
                table: "HypixelGuildStats",
                column: "SkyblockExperience_Total");

            migrationBuilder.CreateIndex(
                name: "IX_HypixelGuildStats_SlayerExperience_Total",
                table: "HypixelGuildStats",
                column: "SlayerExperience_Total");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameModeHistories");

            migrationBuilder.DropTable(
                name: "HypixelGuildStats");

            migrationBuilder.DropIndex(
                name: "IX_HypixelGuilds_MemberCount",
                table: "HypixelGuilds");

            migrationBuilder.DropIndex(
                name: "IX_HypixelGuildMembers_Active",
                table: "HypixelGuildMembers");

            migrationBuilder.DropIndex(
                name: "IX_HypixelGuildMembers_GuildId_Active",
                table: "HypixelGuildMembers");

            migrationBuilder.DropIndex(
                name: "IX_HypixelGuildMembers_GuildId_PlayerUuid",
                table: "HypixelGuildMembers");

            migrationBuilder.DropColumn(
                name: "Slayers",
                table: "ProfileMembers");

            migrationBuilder.DropColumn(
                name: "MemberCount",
                table: "HypixelGuilds");

            migrationBuilder.DropColumn(
                name: "LeftAt",
                table: "HypixelGuildMembers");

            migrationBuilder.CreateIndex(
                name: "IX_HypixelGuildMembers_GuildId",
                table: "HypixelGuildMembers",
                column: "GuildId");
        }
    }
}
