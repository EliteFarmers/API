using System;
using System.Collections.Generic;
using EliteAPI.Models.Entities.Events;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class Events : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Guilds",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Features = table.Column<GuildFeatures>(type: "jsonb", nullable: false),
                    InviteCode = table.Column<string>(type: "text", nullable: true),
                    Banner = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    AdminRole = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Icon = table.Column<string>(type: "text", nullable: true),
                    BotPermissions = table.Column<long>(type: "bigint", nullable: false),
                    BotPermissionsNew = table.Column<string>(type: "text", nullable: false),
                    DiscordFeatures = table.Column<List<string>>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guilds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Events",
                columns: table => new
                {
                    Id = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Rules = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    PrizeInfo = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    Banner = table.Column<string>(type: "text", nullable: true),
                    Thumbnail = table.Column<string>(type: "text", nullable: true),
                    StartTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DynamicStartTime = table.Column<bool>(type: "boolean", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    BlockedUsers = table.Column<List<BlockedUser>>(type: "jsonb", nullable: false),
                    OwnerId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    GuildId = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Events_Accounts_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Events_Guilds_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guilds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EventMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    Collected = table.Column<long>(type: "bigint", nullable: false),
                    StartConditions = table.Column<EventMemberStartConditions>(type: "jsonb", nullable: false),
                    LastUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    StartTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Disqualified = table.Column<bool>(type: "boolean", nullable: false),
                    Reason = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ProfileMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    UserId = table.Column<decimal>(type: "numeric(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventMembers_Accounts_UserId",
                        column: x => x.UserId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventMembers_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EventMembers_ProfileMembers_ProfileMemberId",
                        column: x => x.ProfileMemberId,
                        principalTable: "ProfileMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventMembers_EventId",
                table: "EventMembers",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventMembers_ProfileMemberId",
                table: "EventMembers",
                column: "ProfileMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_EventMembers_UserId",
                table: "EventMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_GuildId",
                table: "Events",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Events_OwnerId",
                table: "Events",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventMembers");

            migrationBuilder.DropTable(
                name: "Events");

            migrationBuilder.DropTable(
                name: "Guilds");
        }
    }
}
