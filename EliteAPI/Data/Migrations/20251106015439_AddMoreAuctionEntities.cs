using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreAuctionEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuctionPriceHistories",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SkyblockId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    VariantKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false, defaultValue: ""),
                    BucketStart = table.Column<long>(type: "bigint", nullable: false),
                    LowestBinPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    AverageBinPrice = table.Column<decimal>(type: "numeric", nullable: true),
                    BinListings = table.Column<int>(type: "integer", nullable: false),
                    LowestSalePrice = table.Column<decimal>(type: "numeric", nullable: true),
                    AverageSalePrice = table.Column<decimal>(type: "numeric", nullable: true),
                    SaleAuctions = table.Column<int>(type: "integer", nullable: false),
                    ItemsSold = table.Column<int>(type: "integer", nullable: false),
                    CalculatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuctionPriceHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuctionSubscription",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    ProfileMemberId = table.Column<Guid>(type: "uuid", nullable: false),
                    PausedUntil = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuctionSubscription", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuctionSubscription_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AuctionSubscription_ProfileMembers_ProfileMemberId",
                        column: x => x.ProfileMemberId,
                        principalTable: "ProfileMembers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EndedAuctions",
                columns: table => new
                {
                    AuctionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerUuid = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerProfileUuid = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerProfileMemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    BuyerUuid = table.Column<Guid>(type: "uuid", nullable: false),
                    BuyerProfileUuid = table.Column<Guid>(type: "uuid", nullable: false),
                    BuyerProfileMemberId = table.Column<Guid>(type: "uuid", nullable: true),
                    Timestamp = table.Column<long>(type: "bigint", nullable: false),
                    Price = table.Column<long>(type: "bigint", nullable: false),
                    Count = table.Column<short>(type: "smallint", nullable: false),
                    Bin = table.Column<bool>(type: "boolean", nullable: false),
                    ItemUuid = table.Column<Guid>(type: "uuid", nullable: true),
                    SkyblockId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    VariantKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false, defaultValue: ""),
                    Item = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EndedAuctions", x => x.AuctionId);
                    table.ForeignKey(
                        name: "FK_EndedAuctions_ProfileMembers_BuyerProfileMemberId",
                        column: x => x.BuyerProfileMemberId,
                        principalTable: "ProfileMembers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_EndedAuctions_ProfileMembers_SellerProfileMemberId",
                        column: x => x.SellerProfileMemberId,
                        principalTable: "ProfileMembers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuctionPriceHistories_BucketStart",
                table: "AuctionPriceHistories",
                column: "BucketStart");

            migrationBuilder.CreateIndex(
                name: "IX_AuctionPriceHistories_SkyblockId_VariantKey_BucketStart",
                table: "AuctionPriceHistories",
                columns: new[] { "SkyblockId", "VariantKey", "BucketStart" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuctionSubscription_AccountId",
                table: "AuctionSubscription",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_AuctionSubscription_ProfileMemberId",
                table: "AuctionSubscription",
                column: "ProfileMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_EndedAuctions_BuyerProfileMemberId",
                table: "EndedAuctions",
                column: "BuyerProfileMemberId");

            migrationBuilder.CreateIndex(
                name: "IX_EndedAuctions_Price",
                table: "EndedAuctions",
                column: "Price");

            migrationBuilder.CreateIndex(
                name: "IX_EndedAuctions_SellerProfileMemberId",
                table: "EndedAuctions",
                column: "SellerProfileMemberId");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuctionPriceHistories");

            migrationBuilder.DropTable(
                name: "AuctionSubscription");

            migrationBuilder.DropTable(
                name: "EndedAuctions");
        }
    }
}
