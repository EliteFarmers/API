using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddChocolateFactoryData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChocolateFactories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Chocolate = table.Column<long>(type: "bigint", nullable: false),
                    TotalChocolate = table.Column<long>(type: "bigint", nullable: false),
                    ChocolateSincePrestige = table.Column<long>(type: "bigint", nullable: false),
                    ChocolateSpent = table.Column<long>(type: "bigint", nullable: false),
                    LastViewedChocolateFactory = table.Column<long>(type: "bigint", nullable: false),
                    Prestige = table.Column<int>(type: "integer", nullable: false),
                    UniqueRabbits_Common = table.Column<int>(type: "integer", nullable: false),
                    UniqueRabbits_Uncommon = table.Column<int>(type: "integer", nullable: false),
                    UniqueRabbits_Rare = table.Column<int>(type: "integer", nullable: false),
                    UniqueRabbits_Epic = table.Column<int>(type: "integer", nullable: false),
                    UniqueRabbits_Legendary = table.Column<int>(type: "integer", nullable: false),
                    UniqueRabbits_Mythic = table.Column<int>(type: "integer", nullable: false),
                    UniqueRabbits_Divine = table.Column<int>(type: "integer", nullable: false),
                    TotalRabbits_Common = table.Column<int>(type: "integer", nullable: false),
                    TotalRabbits_Uncommon = table.Column<int>(type: "integer", nullable: false),
                    TotalRabbits_Rare = table.Column<int>(type: "integer", nullable: false),
                    TotalRabbits_Epic = table.Column<int>(type: "integer", nullable: false),
                    TotalRabbits_Legendary = table.Column<int>(type: "integer", nullable: false),
                    TotalRabbits_Mythic = table.Column<int>(type: "integer", nullable: false),
                    TotalRabbits_Divine = table.Column<int>(type: "integer", nullable: false),
                    ProfileMemberId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChocolateFactories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ChocolateFactories_ProfileMembers_ProfileMemberId",
                        column: x => x.ProfileMemberId,
                        principalTable: "ProfileMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChocolateFactories_ProfileMemberId",
                table: "ChocolateFactories",
                column: "ProfileMemberId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChocolateFactories");
        }
    }
}
