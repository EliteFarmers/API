using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class Jacob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_DiscordAccounts_DiscordAccountId",
                table: "Accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_Collections_Members_MemberId",
                table: "Collections");

            migrationBuilder.DropForeignKey(
                name: "FK_Profiles_MinecraftAccounts_MinecraftAccountId",
                table: "Profiles");

            migrationBuilder.DropTable(
                name: "Members");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MinecraftAccounts",
                table: "MinecraftAccounts");

            migrationBuilder.DropIndex(
                name: "IX_Collections_MemberId",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "MemberId",
                table: "Collections");

            migrationBuilder.AlterColumn<int>(
                name: "MinecraftAccountId",
                table: "Profiles",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinecraftAccountId",
                table: "MinecraftAccounts",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AlterColumn<decimal>(
                name: "Id",
                table: "DiscordAccounts",
                type: "numeric(20,0)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "ProfileMemberId",
                table: "Collections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<decimal>(
                name: "DiscordAccountId",
                table: "Accounts",
                type: "numeric(20,0)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_MinecraftAccounts",
                table: "MinecraftAccounts",
                column: "MinecraftAccountId");

            migrationBuilder.CreateTable(
                name: "JacobContests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Crop = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JacobContests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProfileMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerDataId = table.Column<int>(type: "integer", nullable: false),
                    MinecraftAccountId = table.Column<int>(type: "integer", nullable: false),
                    ProfileId = table.Column<int>(type: "integer", nullable: false)
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
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JacobData",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
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
                name: "ContestParticipations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Collected = table.Column<int>(type: "integer", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    MedalEarned = table.Column<int>(type: "integer", nullable: false),
                    JacobContestId = table.Column<int>(type: "integer", nullable: false),
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
                });

            migrationBuilder.CreateTable(
                name: "EarnedMedalInventory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Bronze = table.Column<int>(type: "integer", nullable: false),
                    Silver = table.Column<int>(type: "integer", nullable: false),
                    Gold = table.Column<int>(type: "integer", nullable: false),
                    JacobDataId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EarnedMedalInventory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EarnedMedalInventory_JacobData_JacobDataId",
                        column: x => x.JacobDataId,
                        principalTable: "JacobData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JacobPerks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DoubleDrops = table.Column<int>(type: "integer", nullable: false),
                    LevelCap = table.Column<int>(type: "integer", nullable: false),
                    JacobDataId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JacobPerks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JacobPerks_JacobData_JacobDataId",
                        column: x => x.JacobDataId,
                        principalTable: "JacobData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MedalInventories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Bronze = table.Column<int>(type: "integer", nullable: false),
                    Silver = table.Column<int>(type: "integer", nullable: false),
                    Gold = table.Column<int>(type: "integer", nullable: false),
                    JacobDataId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedalInventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MedalInventories_JacobData_JacobDataId",
                        column: x => x.JacobDataId,
                        principalTable: "JacobData",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "IX_EarnedMedalInventory_JacobDataId",
                table: "EarnedMedalInventory",
                column: "JacobDataId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JacobData_ProfileMemberId",
                table: "JacobData",
                column: "ProfileMemberId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JacobPerks_JacobDataId",
                table: "JacobPerks",
                column: "JacobDataId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedalInventories_JacobDataId",
                table: "MedalInventories",
                column: "JacobDataId",
                unique: true);

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

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_DiscordAccounts_DiscordAccountId",
                table: "Accounts",
                column: "DiscordAccountId",
                principalTable: "DiscordAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Collections_ProfileMembers_ProfileMemberId",
                table: "Collections",
                column: "ProfileMemberId",
                principalTable: "ProfileMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Profiles_MinecraftAccounts_MinecraftAccountId",
                table: "Profiles",
                column: "MinecraftAccountId",
                principalTable: "MinecraftAccounts",
                principalColumn: "MinecraftAccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Accounts_DiscordAccounts_DiscordAccountId",
                table: "Accounts");

            migrationBuilder.DropForeignKey(
                name: "FK_Collections_ProfileMembers_ProfileMemberId",
                table: "Collections");

            migrationBuilder.DropForeignKey(
                name: "FK_Profiles_MinecraftAccounts_MinecraftAccountId",
                table: "Profiles");

            migrationBuilder.DropTable(
                name: "ContestParticipations");

            migrationBuilder.DropTable(
                name: "EarnedMedalInventory");

            migrationBuilder.DropTable(
                name: "JacobPerks");

            migrationBuilder.DropTable(
                name: "MedalInventories");

            migrationBuilder.DropTable(
                name: "JacobContests");

            migrationBuilder.DropTable(
                name: "JacobData");

            migrationBuilder.DropTable(
                name: "ProfileMembers");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MinecraftAccounts",
                table: "MinecraftAccounts");

            migrationBuilder.DropIndex(
                name: "IX_Collections_ProfileMemberId",
                table: "Collections");

            migrationBuilder.DropColumn(
                name: "MinecraftAccountId",
                table: "MinecraftAccounts");

            migrationBuilder.DropColumn(
                name: "ProfileMemberId",
                table: "Collections");

            migrationBuilder.AlterColumn<string>(
                name: "MinecraftAccountId",
                table: "Profiles",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Id",
                table: "DiscordAccounts",
                type: "text",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AddColumn<int>(
                name: "MemberId",
                table: "Collections",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DiscordAccountId",
                table: "Accounts",
                type: "text",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(20,0)");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MinecraftAccounts",
                table: "MinecraftAccounts",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "Members",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfileId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Members", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Members_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Collections_MemberId",
                table: "Collections",
                column: "MemberId");

            migrationBuilder.CreateIndex(
                name: "IX_Members_ProfileId",
                table: "Members",
                column: "ProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Accounts_DiscordAccounts_DiscordAccountId",
                table: "Accounts",
                column: "DiscordAccountId",
                principalTable: "DiscordAccounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Collections_Members_MemberId",
                table: "Collections",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Profiles_MinecraftAccounts_MinecraftAccountId",
                table: "Profiles",
                column: "MinecraftAccountId",
                principalTable: "MinecraftAccounts",
                principalColumn: "Id");
        }
    }
}
