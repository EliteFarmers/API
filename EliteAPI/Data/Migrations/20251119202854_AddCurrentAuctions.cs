using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCurrentAuctions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EndedAuctions_SkyblockId_Timestamp",
                table: "EndedAuctions");

            migrationBuilder.DropIndex(
                name: "IX_EndedAuctions_SkyblockId_VariantKey_Timestamp",
                table: "EndedAuctions");

            migrationBuilder.DropIndex(
                name: "IX_EndedAuctions_Timestamp",
                table: "EndedAuctions");
            
            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "EndedAuctions",
                newName: "SoldAt");

            migrationBuilder.AlterColumn<Guid>(
                name: "BuyerUuid",
                table: "EndedAuctions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "BuyerProfileUuid",
                table: "EndedAuctions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<long>(
                name: "End",
                table: "EndedAuctions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "HighestBid",
                table: "EndedAuctions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastUpdatedAt",
                table: "EndedAuctions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<long>(
                name: "StartingBid",
                table: "EndedAuctions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "Start",
                table: "EndedAuctions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateIndex(
                name: "IX_EndedAuctions_SkyblockId_SoldAt",
                table: "EndedAuctions",
                columns: new[] { "SkyblockId", "SoldAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EndedAuctions_SkyblockId_VariantKey_SoldAt",
                table: "EndedAuctions",
                columns: new[] { "SkyblockId", "VariantKey", "SoldAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EndedAuctions_SoldAt",
                table: "EndedAuctions",
                column: "SoldAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EndedAuctions_SkyblockId_SoldAt",
                table: "EndedAuctions");

            migrationBuilder.DropIndex(
                name: "IX_EndedAuctions_SkyblockId_VariantKey_SoldAt",
                table: "EndedAuctions");

            migrationBuilder.DropIndex(
                name: "IX_EndedAuctions_SoldAt",
                table: "EndedAuctions");

            migrationBuilder.DropColumn(
                name: "End",
                table: "EndedAuctions");

            migrationBuilder.DropColumn(
                name: "HighestBid",
                table: "EndedAuctions");

            migrationBuilder.DropColumn(
                name: "LastUpdatedAt",
                table: "EndedAuctions");

            migrationBuilder.DropColumn(
                name: "SoldAt",
                table: "EndedAuctions");

            migrationBuilder.DropColumn(
                name: "Start",
                table: "EndedAuctions");

            migrationBuilder.RenameColumn(
                name: "SoldAt",
                table: "EndedAuctions",
                newName: "Timestamp");

            migrationBuilder.AlterColumn<Guid>(
                name: "BuyerUuid",
                table: "EndedAuctions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "BuyerProfileUuid",
                table: "EndedAuctions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EndedAuctions_SkyblockId_Timestamp",
                table: "EndedAuctions",
                columns: new[] { "SkyblockId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_EndedAuctions_SkyblockId_VariantKey_Timestamp",
                table: "EndedAuctions",
                columns: new[] { "SkyblockId", "VariantKey", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_EndedAuctions_Timestamp",
                table: "EndedAuctions",
                column: "Timestamp");
        }
    }
}
