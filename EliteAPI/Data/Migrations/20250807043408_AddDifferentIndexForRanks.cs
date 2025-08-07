using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDifferentIndexForRanks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LeaderboardEntries_Rank_Subquery",
                table: "LeaderboardEntries");

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardEntries_Ranks_Subquery",
                table: "LeaderboardEntries",
                columns: new[] { "LeaderboardId", "IsRemoved", "IntervalIdentifier", "Score" },
                descending: new[] { false, false, false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LeaderboardEntries_Ranks_Subquery",
                table: "LeaderboardEntries");

            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardEntries_Rank_Subquery",
                table: "LeaderboardEntries",
                columns: new[] { "LeaderboardId", "IntervalIdentifier", "IsRemoved", "Score" },
                descending: new[] { false, false, false, true });
        }
    }
}
