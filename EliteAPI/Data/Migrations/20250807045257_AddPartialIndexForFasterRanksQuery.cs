using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPartialIndexForFasterRanksQuery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_LeaderboardEntries_Rank_Subquery_AllTime",
                table: "LeaderboardEntries",
                columns: new[] { "LeaderboardId", "IsRemoved", "Score" },
                descending: new[] { false, false, true },
                filter: "\"IntervalIdentifier\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LeaderboardEntries_Rank_Subquery_AllTime",
                table: "LeaderboardEntries");
        }
    }
}
