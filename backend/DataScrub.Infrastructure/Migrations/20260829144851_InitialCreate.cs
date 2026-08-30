using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DataScrub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Datasets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OriginalFilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CleanedFilePath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RowCount = table.Column<int>(type: "int", nullable: false),
                    ColumnCount = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Datasets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DetectedIssues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DatasetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RowIndex = table.Column<int>(type: "int", nullable: false),
                    RelatedRowIndex = table.Column<int>(type: "int", nullable: true),
                    ColumnName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OriginalValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SuggestedValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConfidenceScore = table.Column<double>(type: "float", nullable: false),
                    Resolution = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetectedIssues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetectedIssues_Datasets_DatasetId",
                        column: x => x.DatasetId,
                        principalTable: "Datasets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetectedIssues_DatasetId",
                table: "DetectedIssues",
                column: "DatasetId");

            migrationBuilder.CreateIndex(
                name: "IX_DetectedIssues_Resolution",
                table: "DetectedIssues",
                column: "Resolution");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetectedIssues");

            migrationBuilder.DropTable(
                name: "Datasets");
        }
    }
}
