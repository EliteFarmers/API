using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventMemberEstimatedTime : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "EstimatedTimeActive",
                table: "EventMembers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EstimatedTimeActive",
                table: "EventMembers");
        }
    }
}
