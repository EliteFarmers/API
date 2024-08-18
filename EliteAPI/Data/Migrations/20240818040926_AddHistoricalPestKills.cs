using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EliteAPI.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHistoricalPestKills : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Beetle",
                table: "CropCollections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Cricket",
                table: "CropCollections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Earthworm",
                table: "CropCollections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Fly",
                table: "CropCollections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Locust",
                table: "CropCollections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Mite",
                table: "CropCollections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Mosquito",
                table: "CropCollections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Moth",
                table: "CropCollections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Rat",
                table: "CropCollections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Slug",
                table: "CropCollections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

//              migrationBuilder.Sql(
//                  $"""
//                      SELECT create_hypertable('"CropCollections"', 'Time', migrate_data => true);
//                  
//                      SELECT set_chunk_time_interval('"CropCollections"', INTERVAL '24 hours');
//                      ALTER TABLE "CropCollections" SET (
//                          timescaledb.compress = true,
//                          timescaledb.compress_segmentby = '"ProfileMemberId"',
//                          timescaledb.compress_chunk_time_interval = '24 hours'
//                      );
//                      
//                      SELECT add_compression_policy('"CropCollections"', INTERVAL '1 month');
//                  """);
//             
//             migrationBuilder.Sql(
//                 $"""
//                      SELECT create_hypertable('"SkillExperiences"', 'Time', migrate_data => true);
//                  
//                      SELECT set_chunk_time_interval('"SkillExperiences"', INTERVAL '24 hours');
//                      ALTER TABLE "SkillExperiences" SET (
//                  	    timescaledb.compress = true,
//                  	    timescaledb.compress_segmentby = '"ProfileMemberId"',
//                  	    timescaledb.compress_chunk_time_interval = '24 hours'
//                      );
//                      
//                      SELECT add_compression_policy('"SkillExperiences"', INTERVAL '1 month');
//                  """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Beetle",
                table: "CropCollections");

            migrationBuilder.DropColumn(
                name: "Cricket",
                table: "CropCollections");

            migrationBuilder.DropColumn(
                name: "Earthworm",
                table: "CropCollections");

            migrationBuilder.DropColumn(
                name: "Fly",
                table: "CropCollections");

            migrationBuilder.DropColumn(
                name: "Locust",
                table: "CropCollections");

            migrationBuilder.DropColumn(
                name: "Mite",
                table: "CropCollections");

            migrationBuilder.DropColumn(
                name: "Mosquito",
                table: "CropCollections");

            migrationBuilder.DropColumn(
                name: "Moth",
                table: "CropCollections");

            migrationBuilder.DropColumn(
                name: "Rat",
                table: "CropCollections");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "CropCollections");
        }
    }
}
