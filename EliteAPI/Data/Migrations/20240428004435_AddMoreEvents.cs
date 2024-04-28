using System;
using EliteAPI.Models.Entities.Events;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Target",
                table: "Events");

            migrationBuilder.RenameColumn(
                name: "Category",
                table: "Events",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "AmountGained",
                table: "EventMembers",
                newName: "Score");

            migrationBuilder.AlterColumn<string>(
                name: "Thumbnail",
                table: "Events",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RequiredRole",
                table: "Events",
                type: "character varying(24)",
                maxLength: 24,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BlockedRole",
                table: "Events",
                type: "character varying(24)",
                maxLength: 24,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Banner",
                table: "Events",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<MedalEventData>(
                name: "Data",
                table: "Events",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "JoinUntilTime",
                table: "Events",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new TimeSpan(0, 0, 0, 0, 0)).ToUniversalTime());

            migrationBuilder.AddColumn<MedalEventMemberData>(
                name: "Data",
                table: "EventMembers",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "EventMembers",
                type: "integer",
                nullable: false,
                defaultValue: 1);
            
            // Populate "JoinUntilTime" with the event end time
            migrationBuilder.Sql($"""UPDATE "Events" SET "JoinUntilTime" = "EndTime";""");
            // Update all old events to the new event type "1"
            migrationBuilder.Sql($"""UPDATE "Events" SET "Type" = 1 WHERE "Type" = 0;""");
            // Update old event members to the new event type "1"
            migrationBuilder.Sql($"""UPDATE "EventMembers" SET "Type" = 1 WHERE "Type" = 0;""");
            // Move "StartConditions" jsonb data to "Data" column
            migrationBuilder.Sql($"""UPDATE "EventMembers" SET "Data" = "StartConditions" WHERE "Type" = 1;""");
            
            // Drop the old "StartConditions" column
            migrationBuilder.DropColumn(
                name: "StartConditions",
                table: "EventMembers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<EventMemberWeightData>(
                name: "StartConditions",
                table: "EventMembers",
                type: "jsonb",
                nullable: false,
                defaultValue: "{}");
            
            // Move "Data" jsonb data to "StartConditions" column
            migrationBuilder.Sql($"""UPDATE "EventMembers" SET "StartConditions" = "Data" WHERE "Type" = 1;""");

            migrationBuilder.DropColumn(
                name: "Data",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "JoinUntilTime",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Data",
                table: "EventMembers");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "EventMembers");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Events",
                newName: "Category");

            migrationBuilder.RenameColumn(
                name: "Score",
                table: "EventMembers",
                newName: "AmountGained");

            migrationBuilder.AlterColumn<string>(
                name: "Thumbnail",
                table: "Events",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "RequiredRole",
                table: "Events",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(24)",
                oldMaxLength: 24,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BlockedRole",
                table: "Events",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(24)",
                oldMaxLength: 24,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Banner",
                table: "Events",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1024)",
                oldMaxLength: 1024,
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Target",
                table: "Events",
                type: "text",
                nullable: true);
        }
    }
}
