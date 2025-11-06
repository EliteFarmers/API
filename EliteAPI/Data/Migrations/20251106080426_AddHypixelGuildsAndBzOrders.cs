using System;
using System.Collections.Generic;
using EliteAPI.Features.Resources.Bazaar;
using EliteFarmers.HypixelAPI.DTOs;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHypixelGuildsAndBzOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "GuildLastUpdated",
                table: "MinecraftAccounts",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "GuildMemberId",
                table: "MinecraftAccounts",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<BazaarOrders>(
                name: "Orders",
                table: "BazaarProductSummaries",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.CreateTable(
                name: "HypixelGuilds",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    NameLower = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<long>(type: "bigint", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    PreferredGames = table.Column<List<string>>(type: "jsonb", nullable: true),
                    PubliclyListed = table.Column<bool>(type: "boolean", nullable: false),
                    Public = table.Column<bool>(type: "boolean", nullable: false),
                    Exp = table.Column<long>(type: "bigint", nullable: false),
                    Tag = table.Column<string>(type: "text", nullable: true),
                    TagColor = table.Column<string>(type: "text", nullable: true),
                    GameExp = table.Column<Dictionary<string, long>>(type: "jsonb", nullable: false),
                    Ranks = table.Column<List<RawHypixelGuildRank>>(type: "jsonb", nullable: false),
                    LastUpdated = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HypixelGuilds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HypixelGuildMembers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GuildId = table.Column<string>(type: "text", nullable: false),
                    PlayerUuid = table.Column<string>(type: "text", nullable: false),
                    Rank = table.Column<string>(type: "text", nullable: true),
                    JoinedAt = table.Column<long>(type: "bigint", nullable: false),
                    QuestParticipation = table.Column<int>(type: "integer", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HypixelGuildMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HypixelGuildMembers_HypixelGuilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "HypixelGuilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HypixelGuildMembers_MinecraftAccounts_PlayerUuid",
                        column: x => x.PlayerUuid,
                        principalTable: "MinecraftAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HypixelGuildMemberExps",
                columns: table => new
                {
                    GuildMemberId = table.Column<long>(type: "bigint", nullable: false),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    Xp = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HypixelGuildMemberExps", x => new { x.GuildMemberId, x.Day });
                    table.ForeignKey(
                        name: "FK_HypixelGuildMemberExps_HypixelGuildMembers_GuildMemberId",
                        column: x => x.GuildMemberId,
                        principalTable: "HypixelGuildMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MinecraftAccounts_GuildMemberId",
                table: "MinecraftAccounts",
                column: "GuildMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_HypixelGuildMemberExps_Day",
                table: "HypixelGuildMemberExps",
                column: "Day",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_HypixelGuildMemberExps_GuildMemberId",
                table: "HypixelGuildMemberExps",
                column: "GuildMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_HypixelGuildMembers_GuildId",
                table: "HypixelGuildMembers",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_HypixelGuildMembers_PlayerUuid",
                table: "HypixelGuildMembers",
                column: "PlayerUuid");

            migrationBuilder.CreateIndex(
                name: "IX_HypixelGuilds_NameLower",
                table: "HypixelGuilds",
                column: "NameLower");

            migrationBuilder.AddForeignKey(
                name: "FK_MinecraftAccounts_HypixelGuildMembers_GuildMemberId",
                table: "MinecraftAccounts",
                column: "GuildMemberId",
                principalTable: "HypixelGuildMembers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MinecraftAccounts_HypixelGuildMembers_GuildMemberId",
                table: "MinecraftAccounts");

            migrationBuilder.DropTable(
                name: "HypixelGuildMemberExps");

            migrationBuilder.DropTable(
                name: "HypixelGuildMembers");

            migrationBuilder.DropTable(
                name: "HypixelGuilds");

            migrationBuilder.DropIndex(
                name: "IX_MinecraftAccounts_GuildMemberId",
                table: "MinecraftAccounts");

            migrationBuilder.DropColumn(
                name: "GuildLastUpdated",
                table: "MinecraftAccounts");

            migrationBuilder.DropColumn(
                name: "GuildMemberId",
                table: "MinecraftAccounts");

            migrationBuilder.DropColumn(
                name: "Orders",
                table: "BazaarProductSummaries");
        }
    }
}
