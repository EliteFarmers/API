using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdjustLeaderboardEntryIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LeaderboardEntries_Rank_Subquery_AllTime",
                table: "LeaderboardEntries");

            migrationBuilder.DropIndex(
                name: "IX_LeaderboardEntries_Ranks_Subquery",
                table: "LeaderboardEntries");

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardEntries_Rank_Subquery_AllTime",
                table: "LeaderboardEntries",
                columns: new[] { "LeaderboardId", "IsRemoved", "Score", "LeaderboardEntryId" },
                descending: new[] { false, false, true, true },
                filter: "\"IntervalIdentifier\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardEntries_Ranks_Subquery",
                table: "LeaderboardEntries",
                columns: new[] { "LeaderboardId", "IsRemoved", "IntervalIdentifier", "Score", "LeaderboardEntryId" },
                descending: new[] { false, false, false, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LeaderboardEntries_Rank_Subquery_AllTime",
                table: "LeaderboardEntries");

            migrationBuilder.DropIndex(
                name: "IX_LeaderboardEntries_Ranks_Subquery",
                table: "LeaderboardEntries");

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardEntries_Rank_Subquery_AllTime",
                table: "LeaderboardEntries",
                columns: new[] { "LeaderboardId", "IsRemoved", "Score" },
                descending: new[] { false, false, true },
                filter: "\"IntervalIdentifier\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardEntries_Ranks_Subquery",
                table: "LeaderboardEntries",
                columns: new[] { "LeaderboardId", "IsRemoved", "IntervalIdentifier", "Score" },
                descending: new[] { false, false, false, true });
        }
    }
}
