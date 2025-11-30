using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFiresaleItemSlot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SkyblockFiresaleItems",
                table: "SkyblockFiresaleItems");

            migrationBuilder.AddColumn<int>(
                name: "SlotId",
                table: "SkyblockFiresaleItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);
            
            // Set slot ids to index of items within each firesale
            migrationBuilder.Sql(@"
                WITH ranked AS (
                    SELECT
                        ""FiresaleId"",
                        ""ItemId"",
                        ROW_NUMBER() OVER (PARTITION BY ""FiresaleId"" ORDER BY ""ItemId"") - 1 AS ""SlotId""
                    FROM ""SkyblockFiresaleItems""
                )
                UPDATE ""SkyblockFiresaleItems"" sfi
                SET ""SlotId"" = r.""SlotId""
                FROM ranked r
                WHERE sfi.""FiresaleId"" = r.""FiresaleId"" AND sfi.""ItemId"" = r.""ItemId"";
            ");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SkyblockFiresaleItems",
                table: "SkyblockFiresaleItems",
                columns: new[] { "FiresaleId", "SlotId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_SkyblockFiresaleItems",
                table: "SkyblockFiresaleItems");

            migrationBuilder.DropColumn(
                name: "SlotId",
                table: "SkyblockFiresaleItems");
            
            // Remove entries that would conflict with this primary key
            // Destructive, but they couldn't exist with this setup anyway
            migrationBuilder.Sql(@"
                DELETE FROM ""SkyblockFiresaleItems""
                WHERE ctid NOT IN (
                    SELECT min(ctid)
                    FROM ""SkyblockFiresaleItems""
                    GROUP BY ""FiresaleId"", ""ItemId""
                );
            ");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SkyblockFiresaleItems",
                table: "SkyblockFiresaleItems",
                columns: new[] { "FiresaleId", "ItemId" });
        }
    }
}
