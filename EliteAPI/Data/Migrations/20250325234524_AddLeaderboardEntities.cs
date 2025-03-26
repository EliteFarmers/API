using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLeaderboardEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Leaderboards",
                columns: table => new
                {
                    LeaderboardId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Slug = table.Column<string>(type: "text", nullable: false),
                    IntervalType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EntryType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ScoreDataType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IconId = table.Column<string>(type: "character varying(48)", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ShortTitle = table.Column<string>(type: "text", nullable: true),
                    Property = table.Column<string>(type: "text", nullable: true),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Leaderboards", x => x.LeaderboardId);
                    table.ForeignKey(
                        name: "FK_Leaderboards_Images_IconId",
                        column: x => x.IconId,
                        principalTable: "Images",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ProfileMemberMetadata",
                columns: table => new
                {
                    ProfileMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Uuid = table.Column<string>(type: "text", nullable: false),
                    Prefix = table.Column<string>(type: "text", nullable: true),
                    Profile = table.Column<string>(type: "text", nullable: false),
                    ProfileUuid = table.Column<string>(type: "text", nullable: false),
                    SkyblockExperience = table.Column<int>(type: "integer", nullable: false),
                    Cosmetics = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfileMemberMetadata", x => x.ProfileMemberId);
                    table.ForeignKey(
                        name: "FK_ProfileMemberMetadata_ProfileMembers_ProfileMemberId",
                        column: x => x.ProfileMemberId,
                        principalTable: "ProfileMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeaderboardEntries",
                columns: table => new
                {
                    LeaderboardEntryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeaderboardId = table.Column<int>(type: "integer", nullable: false),
                    IntervalIdentifier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProfileId = table.Column<string>(type: "text", nullable: true),
                    ProfileMemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    InitialScore = table.Column<decimal>(type: "numeric(24,4)", nullable: false, defaultValue: 0m),
                    Score = table.Column<decimal>(type: "numeric(24,4)", nullable: false),
                    IsRemoved = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ProfileType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaderboardEntries", x => x.LeaderboardEntryId);
                    table.ForeignKey(
                        name: "FK_LeaderboardEntries_Leaderboards_LeaderboardId",
                        column: x => x.LeaderboardId,
                        principalTable: "Leaderboards",
                        principalColumn: "LeaderboardId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LeaderboardEntries_ProfileMembers_ProfileMemberId",
                        column: x => x.ProfileMemberId,
                        principalTable: "ProfileMembers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LeaderboardEntries_Profiles_ProfileId",
                        column: x => x.ProfileId,
                        principalTable: "Profiles",
                        principalColumn: "ProfileId");
                });

            migrationBuilder.CreateTable(
                name: "LeaderboardSnapshots",
                columns: table => new
                {
                    LeaderboardSnapshotId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeaderboardId = table.Column<int>(type: "integer", nullable: false),
                    SnapshotTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IntervalIdentifier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaderboardSnapshots", x => x.LeaderboardSnapshotId);
                    table.ForeignKey(
                        name: "FK_LeaderboardSnapshots_Leaderboards_LeaderboardId",
                        column: x => x.LeaderboardId,
                        principalTable: "Leaderboards",
                        principalColumn: "LeaderboardId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LeaderboardSnapshotEntries",
                columns: table => new
                {
                    LeaderboardSnapshotEntryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    LeaderboardSnapshotId = table.Column<int>(type: "integer", nullable: false),
                    IntervalIdentifier = table.Column<string>(type: "text", nullable: true),
                    ProfileId = table.Column<string>(type: "text", nullable: true),
                    ProfileMemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    InitialScore = table.Column<decimal>(type: "numeric", nullable: false),
                    Score = table.Column<decimal>(type: "numeric(24,4)", nullable: false),
                    IsRemoved = table.Column<bool>(type: "boolean", nullable: false),
                    ProfileType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EntryTimestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaderboardSnapshotEntries", x => x.LeaderboardSnapshotEntryId);
                    table.CheckConstraint("CK_LeaderboardSnapshotEntries_ProfileOrMember", "((\"ProfileId\" IS NOT NULL AND \"ProfileMemberId\" IS NULL) OR (\"ProfileId\" IS NULL AND \"ProfileMemberId\" IS NOT NULL))");
                    table.ForeignKey(
                        name: "FK_LeaderboardSnapshotEntries_LeaderboardSnapshots_Leaderboard~",
                        column: x => x.LeaderboardSnapshotId,
                        principalTable: "LeaderboardSnapshots",
                        principalColumn: "LeaderboardSnapshotId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardEntries_IsRemoved",
                table: "LeaderboardEntries",
                column: "IsRemoved");

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardEntries_LeaderboardId_IntervalIdentifier_Score",
                table: "LeaderboardEntries",
                columns: new[] { "LeaderboardId", "IntervalIdentifier", "Score" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardEntries_ProfileId",
                table: "LeaderboardEntries",
                column: "ProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardEntries_ProfileMemberId",
                table: "LeaderboardEntries",
                column: "ProfileMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardEntries_ProfileType_LeaderboardId_IntervalIdenti~",
                table: "LeaderboardEntries",
                columns: new[] { "ProfileType", "LeaderboardId", "IntervalIdentifier" });

            migrationBuilder.CreateIndex(
                name: "IX_Leaderboards_IconId",
                table: "Leaderboards",
                column: "IconId");

            migrationBuilder.CreateIndex(
                name: "IX_Leaderboards_Slug",
                table: "Leaderboards",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardSnapshotEntries_LeaderboardSnapshotId_Score",
                table: "LeaderboardSnapshotEntries",
                columns: new[] { "LeaderboardSnapshotId", "Score" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardSnapshotEntries_ProfileType_LeaderboardSnapshotId",
                table: "LeaderboardSnapshotEntries",
                columns: new[] { "ProfileType", "LeaderboardSnapshotId" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardSnapshots_Definition_Timestamp_Interval",
                table: "LeaderboardSnapshots",
                columns: new[] { "LeaderboardId", "SnapshotTimestamp", "IntervalIdentifier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardSnapshots_LeaderboardId_IntervalIdentifier",
                table: "LeaderboardSnapshots",
                columns: new[] { "LeaderboardId", "IntervalIdentifier" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LeaderboardEntries");

            migrationBuilder.DropTable(
                name: "LeaderboardSnapshotEntries");

            migrationBuilder.DropTable(
                name: "ProfileMemberMetadata");

            migrationBuilder.DropTable(
                name: "LeaderboardSnapshots");

            migrationBuilder.DropTable(
                name: "Leaderboards");
        }
    }
}
