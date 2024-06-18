using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamEventSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxTeamMembers",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxTeams",
                table: "Events",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TeamId",
                table: "EventMembers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EventTeams",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Color = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: true),
                    JoinCode = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    UserId = table.Column<string>(type: "character varying(18)", maxLength: 18, nullable: false),
                    EventId = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventTeams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventTeams_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventMembers_TeamId",
                table: "EventMembers",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_EventTeams_EventId",
                table: "EventTeams",
                column: "EventId");

            migrationBuilder.AddForeignKey(
                name: "FK_EventMembers_EventTeams_TeamId",
                table: "EventMembers",
                column: "TeamId",
                principalTable: "EventTeams",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EventMembers_EventTeams_TeamId",
                table: "EventMembers");

            migrationBuilder.DropTable(
                name: "EventTeams");

            migrationBuilder.DropIndex(
                name: "IX_EventMembers_TeamId",
                table: "EventMembers");

            migrationBuilder.DropColumn(
                name: "MaxTeamMembers",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "MaxTeams",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "TeamId",
                table: "EventMembers");
        }
    }
}
