using System;
using System.Collections.Generic;
using EliteAPI.Models.Entities;
using EliteAPI.Models.Entities.Hypixel;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Permissions = table.Column<int>(type: "integer", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Discriminator = table.Column<string>(type: "text", nullable: true),
                    Avatar = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Locale = table.Column<string>(type: "text", nullable: true),
                    Purchases = table.Column<List<Purchase>>(type: "jsonb", nullable: false),
                    Redemptions = table.Column<List<Redemption>>(type: "jsonb", nullable: false),
                    Inventory = table.Column<EliteInventory>(type: "jsonb", nullable: false),
                    Settings = table.Column<EliteSettings>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JacobContests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Crop = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<long>(type: "bigint", nullable: false),
                    Participants = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JacobContests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PlayerData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Rank = table.Column<string>(type: "text", nullable: true),
                    NewPackageRank = table.Column<string>(type: "text", nullable: true),
                    MonthlyPackageRank = table.Column<string>(type: "text", nullable: true),
                    RankPlusColor = table.Column<string>(type: "text", nullable: true),
                    SocialMedia = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerData", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Profiles",
                columns: table => new
                {
                    ProfileId = table.Column<string>(type: "text", nullable: false),
                    ProfileName = table.Column<string>(type: "text", nullable: false),
                    GameMode = table.Column<string>(type: "text", nullable: true),
                    LastSave = table.Column<long>(type: "bigint", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    Banking = table.Column<ProfileBanking>(type: "jsonb", nullable: false),
                    CraftedMinions = table.Column<Dictionary<string, int>>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiles", x => x.ProfileId);
                });

            migrationBuilder.CreateTable(
                name: "MinecraftAccounts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    PlayerDataId = table.Column<int>(type: "integer", nullable: false),
                    Properties = table.Column<List<MinecraftAccountProperty>>(type: "jsonb", nullable: false),
                    PreviousNames = table.Column<Dictionary<string, long>>(type: "jsonb", nullable: false),
                    AccountId = table.Column<decimal>(type: "numeric(20,0)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MinecraftAccounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MinecraftAccounts_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_MinecraftAccounts_PlayerData_PlayerDataId",
                        column: x => x.PlayerDataId,
                        principalTable: "PlayerData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProfileMembers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SkyblockXp = table.Column<int>(type: "integer", nullable: false),
                    Purse = table.Column<double>(type: "double precision", nullable: false),
                    IsSelected = table.Column<bool>(type: "boolean", nullable: false),
                    WasRemoved = table.Column<bool>(type: "boolean", nullable: false),
                    LastUpdated = table.Column<long>(type: "bigint", nullable: false),
                    Collections = table.Column<Dictionary<string, long>>(type: "jsonb", nullable: false),
                    CollectionTiers = table.Column<Dictionary<string, int>>(type: "jsonb", nullable: false),
                    Stats = table.Column<Dictionary<string, double>>(type: "jsonb", nullable: false),
                    Essence = table.Column<Dictionary<string, int>>(type: "jsonb", nullable: false),
                    Pets = table.Column<List<Pet>>(type: "jsonb", nullable: false),
                    PlayerUuid = table.Column<string>(type: "text", nullable: false),
                    ProfileId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfileMembers_MinecraftAccounts_PlayerUuid",
                        column: x => x.PlayerUuid,
                        principalTable: "MinecraftAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProfileMembers_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "ProfileId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JacobData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Medals_Bronze = table.Column<int>(type: "integer", nullable: false),
                    Medals_Silver = table.Column<int>(type: "integer", nullable: false),
                    Medals_Gold = table.Column<int>(type: "integer", nullable: false),
                    EarnedMedals_Bronze = table.Column<int>(type: "integer", nullable: false),
                    EarnedMedals_Silver = table.Column<int>(type: "integer", nullable: false),
                    EarnedMedals_Gold = table.Column<int>(type: "integer", nullable: false),
                    Perks_DoubleDrops = table.Column<int>(type: "integer", nullable: false),
                    Perks_LevelCap = table.Column<int>(type: "integer", nullable: false),
                    Participations = table.Column<int>(type: "integer", nullable: false),
                    ContestsLastUpdated = table.Column<long>(type: "bigint", nullable: false),
                    ProfileMemberId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JacobData", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JacobData_ProfileMembers_ProfileMemberId",
                        column: x => x.ProfileMemberId,
                        principalTable: "ProfileMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Skills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Combat = table.Column<double>(type: "double precision", nullable: false),
                    Mining = table.Column<double>(type: "double precision", nullable: false),
                    Foraging = table.Column<double>(type: "double precision", nullable: false),
                    Fishing = table.Column<double>(type: "double precision", nullable: false),
                    Enchanting = table.Column<double>(type: "double precision", nullable: false),
                    Alchemy = table.Column<double>(type: "double precision", nullable: false),
                    Carpentry = table.Column<double>(type: "double precision", nullable: false),
                    Runecrafting = table.Column<double>(type: "double precision", nullable: false),
                    Taming = table.Column<double>(type: "double precision", nullable: false),
                    Farming = table.Column<double>(type: "double precision", nullable: false),
                    Social = table.Column<double>(type: "double precision", nullable: false),
                    ProfileMemberId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Skills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Skills_ProfileMembers_ProfileMemberId",
                        column: x => x.ProfileMemberId,
                        principalTable: "ProfileMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContestParticipations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Collected = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    MedalEarned = table.Column<int>(type: "integer", nullable: false),
                    ProfileMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    JacobContestId = table.Column<long>(type: "bigint", nullable: false),
                    JacobDataId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContestParticipations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContestParticipations_JacobContests_JacobContestId",
                        column: x => x.JacobContestId,
                        principalTable: "JacobContests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ContestParticipations_JacobData_JacobDataId",
                        column: x => x.JacobDataId,
                        principalTable: "JacobData",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_ContestParticipations_ProfileMembers_ProfileMemberId",
                        column: x => x.ProfileMemberId,
                        principalTable: "ProfileMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ContestParticipations_JacobContestId",
                table: "ContestParticipations",
                column: "JacobContestId");

            migrationBuilder.CreateIndex(
                name: "IX_ContestParticipations_JacobDataId",
                table: "ContestParticipations",
                column: "JacobDataId");

            migrationBuilder.CreateIndex(
                name: "IX_ContestParticipations_ProfileMemberId",
                table: "ContestParticipations",
                column: "ProfileMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_JacobContests_Timestamp",
                table: "JacobContests",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_JacobData_ProfileMemberId",
                table: "JacobData",
                column: "ProfileMemberId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MinecraftAccounts_AccountId",
                table: "MinecraftAccounts",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_MinecraftAccounts_PlayerDataId",
                table: "MinecraftAccounts",
                column: "PlayerDataId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileMembers_PlayerUuid",
                table: "ProfileMembers",
                column: "PlayerUuid");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileMembers_ProfileId",
                table: "ProfileMembers",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Skills_ProfileMemberId",
                table: "Skills",
                column: "ProfileMemberId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContestParticipations");

            migrationBuilder.DropTable(
                name: "Skills");

            migrationBuilder.DropTable(
                name: "JacobContests");

            migrationBuilder.DropTable(
                name: "JacobData");

            migrationBuilder.DropTable(
                name: "ProfileMembers");

            migrationBuilder.DropTable(
                name: "MinecraftAccounts");

            migrationBuilder.DropTable(
                name: "Profiles");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "PlayerData");
        }
    }
}
