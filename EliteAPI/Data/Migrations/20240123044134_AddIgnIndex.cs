using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIgnIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Inventories");

            migrationBuilder.CreateIndex(
                name: "idx_minecraft_accounts_name",
                table: "MinecraftAccounts",
                column: "Name");
            
            // Add stored procedure to autocomplete IGNs
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION public.autocomplete_igns(start_value text, range_start text, range_end text)
					RETURNS TABLE(ign text) AS $$
					BEGIN
					  RETURN QUERY
					    WITH RECURSIVE distinct_values 
						  (distinct_value) 
						AS (
						  (
							SELECT ""Name""
							FROM ""MinecraftAccounts""
							WHERE (
							  ""Name"" IS NOT NULL
							  AND ""Name"" > start_value
							  AND ""Name"" BETWEEN range_start
											AND range_end     
							)
							ORDER BY 1
							LIMIT 1
						  )
						  UNION ALL
						  (
							SELECT ""Name""
							FROM distinct_values,
							LATERAL (
							  SELECT ""Name""
							  FROM ""MinecraftAccounts""
							  WHERE (
								""Name"" > distinct_value
								AND ""Name"" BETWEEN range_start
											  AND range_end    
							  )
							  ORDER BY 1
							  LIMIT 1
							) X
						  )
						)
						SELECT distinct_value
						FROM distinct_values
						LIMIT 100;
					END;
					$$ LANGUAGE plpgsql;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_minecraft_accounts_name",
                table: "MinecraftAccounts");

            migrationBuilder.CreateTable(
                name: "Inventories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProfileMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    Armor = table.Column<string>(type: "text", nullable: true),
                    Backpacks = table.Column<List<string>>(type: "jsonb", nullable: true),
                    EnderChest = table.Column<string>(type: "text", nullable: true),
                    Equipment = table.Column<string>(type: "text", nullable: true),
                    Inventory = table.Column<string>(type: "text", nullable: true),
                    LastUpdated = table.Column<long>(type: "bigint", nullable: false),
                    PersonalVault = table.Column<string>(type: "text", nullable: true),
                    TalismanBag = table.Column<string>(type: "text", nullable: true),
                    Wardrobe = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inventories_ProfileMembers_ProfileMemberId",
                        column: x => x.ProfileMemberId,
                        principalTable: "ProfileMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_ProfileMemberId",
                table: "Inventories",
                column: "ProfileMemberId");
        }
    }
}
