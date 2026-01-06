using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileMemberHash : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "LastDataChanged",
                table: "ProfileMembers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "ResponseHash",
                table: "ProfileMembers",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "3384aba1-5453-4787-81d9-0b7222225d81",
                column: "ConcurrencyStamp",
                value: "34eb8585-7920-4f4c-857a-e5d131a835ef");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "8270a1b1-5809-436a-ba1c-b712f4f55f67",
                column: "ConcurrencyStamp",
                value: "11d759b6-025f-4334-b8fc-3b26a72cda87");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "d8c803c1-63a0-4594-8d68-aad7bd59df7d",
                column: "ConcurrencyStamp",
                value: "e0e6b08b-7cd1-4f8c-b827-cab959ebc9be");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "e99efab5-3fd2-416e-b8f5-93b0370892ac",
                column: "ConcurrencyStamp",
                value: "c9ad7f78-129a-4507-ace1-5a71c221a901");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "ff4f5319-644e-4332-8bd5-2ec989ba5e7f",
                column: "ConcurrencyStamp",
                value: "e4ec974b-71af-4307-8bf0-3feb9f380566");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastDataChanged",
                table: "ProfileMembers");

            migrationBuilder.DropColumn(
                name: "ResponseHash",
                table: "ProfileMembers");

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "3384aba1-5453-4787-81d9-0b7222225d81",
                column: "ConcurrencyStamp",
                value: null);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "8270a1b1-5809-436a-ba1c-b712f4f55f67",
                column: "ConcurrencyStamp",
                value: null);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "d8c803c1-63a0-4594-8d68-aad7bd59df7d",
                column: "ConcurrencyStamp",
                value: null);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "e99efab5-3fd2-416e-b8f5-93b0370892ac",
                column: "ConcurrencyStamp",
                value: null);

            migrationBuilder.UpdateData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "ff4f5319-644e-4332-8bd5-2ec989ba5e7f",
                column: "ConcurrencyStamp",
                value: null);
        }
    }
}
