using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class Reset : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DiscordAccounts",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    Discriminator = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    Locale = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JacobContestEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JacobContestEvents", x => x.Id);
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
                name: "ProfileBanking",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Balance = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileBanking", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DiscordAccountId = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Accounts_DiscordAccounts_DiscordAccountId",
                        column: x => x.DiscordAccountId,
                        principalTable: "DiscordAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JacobContests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Crop = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    JacobContestEventId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JacobContests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JacobContests_JacobContestEvents_JacobContestEventId",
                        column: x => x.JacobContestEventId,
                        principalTable: "JacobContestEvents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProfileBankingTransaction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Amount = table.Column<double>(type: "double precision", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Initiator = table.Column<string>(type: "text", nullable: true),
                    ProfileBankingId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileBankingTransaction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfileBankingTransaction_ProfileBanking_ProfileBankingId",
                        column: x => x.ProfileBankingId,
                        principalTable: "ProfileBanking",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MinecraftAccounts",
                columns: table => new
                {
                    MinecraftAccountId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Properties = table.Column<string>(type: "text", nullable: false),
                    PlayerDataId = table.Column<int>(type: "integer", nullable: false),
                    AccountId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MinecraftAccounts", x => x.MinecraftAccountId);
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
                name: "PremiumUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    AccountId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PremiumUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PremiumUsers_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Profiles",
                columns: table => new
                {
                    ProfileId = table.Column<string>(type: "text", nullable: false),
                    ProfileName = table.Column<string>(type: "text", nullable: false),
                    GameMode = table.Column<string>(type: "text", nullable: true),
                    LastSave = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BankingId = table.Column<int>(type: "integer", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    MinecraftAccountId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profiles", x => x.ProfileId);
                    table.ForeignKey(
                        name: "FK_Profiles_MinecraftAccounts_MinecraftAccountId",
                        column: x => x.MinecraftAccountId,
                        principalTable: "MinecraftAccounts",
                        principalColumn: "MinecraftAccountId");
                    table.ForeignKey(
                        name: "FK_Profiles_ProfileBanking_BankingId",
                        column: x => x.BankingId,
                        principalTable: "ProfileBanking",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Purchases",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PurchasedTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PurchaseType = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric", nullable: false),
                    PremiumId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Purchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Purchases_PremiumUsers_PremiumId",
                        column: x => x.PremiumId,
                        principalTable: "PremiumUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProfileMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerUuid = table.Column<string>(type: "text", nullable: false),
                    IsSelected = table.Column<bool>(type: "boolean", nullable: false),
                    WasRemoved = table.Column<bool>(type: "boolean", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PlayerDataId = table.Column<int>(type: "integer", nullable: false),
                    MinecraftAccountId = table.Column<int>(type: "integer", nullable: false),
                    ProfileId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfileMembers_MinecraftAccounts_MinecraftAccountId",
                        column: x => x.MinecraftAccountId,
                        principalTable: "MinecraftAccounts",
                        principalColumn: "MinecraftAccountId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProfileMembers_PlayerData_PlayerDataId",
                        column: x => x.PlayerDataId,
                        principalTable: "PlayerData",
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
                name: "Collections",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Amount = table.Column<long>(type: "bigint", nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false),
                    ProfileMemberId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Collections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Collections_ProfileMembers_ProfileMemberId",
                        column: x => x.ProfileMemberId,
                        principalTable: "ProfileMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CraftedMinions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Type = table.Column<string>(type: "text", nullable: true),
                    Tiers = table.Column<int>(type: "integer", nullable: false),
                    ProfileMemberId = table.Column<int>(type: "integer", nullable: false),
                    ProfileId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CraftedMinions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CraftedMinions_ProfileMembers_ProfileMemberId",
                        column: x => x.ProfileMemberId,
                        principalTable: "ProfileMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CraftedMinions_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "ProfileId");
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
                    ProfileMemberId = table.Column<int>(type: "integer", nullable: false)
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
                name: "Pets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UUID = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<string>(type: "text", nullable: true),
                    Exp = table.Column<double>(type: "double precision", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    Tier = table.Column<string>(type: "text", nullable: true),
                    HeldItem = table.Column<string>(type: "text", nullable: true),
                    CandyUsed = table.Column<short>(type: "smallint", nullable: false),
                    Skin = table.Column<string>(type: "text", nullable: true),
                    ProfileMemberId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pets_ProfileMembers_ProfileMemberId",
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
                    Type = table.Column<string>(type: "text", nullable: true),
                    Exp = table.Column<long>(type: "bigint", nullable: false),
                    ProfileMemberId = table.Column<int>(type: "integer", nullable: false)
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
                    JacobContestId = table.Column<int>(type: "integer", nullable: false),
                    ProfileMemberId = table.Column<int>(type: "integer", nullable: false),
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
                name: "IX_Accounts_DiscordAccountId",
                table: "Accounts",
                column: "DiscordAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Collections_ProfileMemberId",
                table: "Collections",
                column: "ProfileMemberId");

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
                name: "IX_CraftedMinions_ProfileId",
                table: "CraftedMinions",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_CraftedMinions_ProfileMemberId",
                table: "CraftedMinions",
                column: "ProfileMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_JacobContests_JacobContestEventId",
                table: "JacobContests",
                column: "JacobContestEventId");

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
                name: "IX_Pets_ProfileMemberId",
                table: "Pets",
                column: "ProfileMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_PremiumUsers_AccountId",
                table: "PremiumUsers",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfileBankingTransaction_ProfileBankingId",
                table: "ProfileBankingTransaction",
                column: "ProfileBankingId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileMembers_MinecraftAccountId",
                table: "ProfileMembers",
                column: "MinecraftAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileMembers_PlayerDataId",
                table: "ProfileMembers",
                column: "PlayerDataId");

            migrationBuilder.CreateIndex(
                name: "IX_ProfileMembers_ProfileId",
                table: "ProfileMembers",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_BankingId",
                table: "Profiles",
                column: "BankingId");

            migrationBuilder.CreateIndex(
                name: "IX_Profiles_MinecraftAccountId",
                table: "Profiles",
                column: "MinecraftAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Purchases_PremiumId",
                table: "Purchases",
                column: "PremiumId");

            migrationBuilder.CreateIndex(
                name: "IX_Skills_ProfileMemberId",
                table: "Skills",
                column: "ProfileMemberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Collections");

            migrationBuilder.DropTable(
                name: "ContestParticipations");

            migrationBuilder.DropTable(
                name: "CraftedMinions");

            migrationBuilder.DropTable(
                name: "Pets");

            migrationBuilder.DropTable(
                name: "ProfileBankingTransaction");

            migrationBuilder.DropTable(
                name: "Purchases");

            migrationBuilder.DropTable(
                name: "Skills");

            migrationBuilder.DropTable(
                name: "JacobContests");

            migrationBuilder.DropTable(
                name: "JacobData");

            migrationBuilder.DropTable(
                name: "PremiumUsers");

            migrationBuilder.DropTable(
                name: "JacobContestEvents");

            migrationBuilder.DropTable(
                name: "ProfileMembers");

            migrationBuilder.DropTable(
                name: "Profiles");

            migrationBuilder.DropTable(
                name: "MinecraftAccounts");

            migrationBuilder.DropTable(
                name: "ProfileBanking");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "PlayerData");

            migrationBuilder.DropTable(
                name: "DiscordAccounts");
        }
    }
}
