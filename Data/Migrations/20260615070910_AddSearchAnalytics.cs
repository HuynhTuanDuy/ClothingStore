using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClothingStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSearchAnalytics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SEARCHLOGS",
                columns: table => new
                {
                    SearchLogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Keyword = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    NormalizedKeyword = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ResultCount = table.Column<int>(type: "int", nullable: false),
                    ElapsedMilliseconds = table.Column<long>(type: "bigint", nullable: false),
                    SearchedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClickedProductId = table.Column<int>(type: "int", nullable: true),
                    ClickedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SEARCHLOGS", x => x.SearchLogId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SEARCHLOGS_NormalizedKeyword",
                table: "SEARCHLOGS",
                column: "NormalizedKeyword");

            migrationBuilder.CreateIndex(
                name: "IX_SEARCHLOGS_ResultCount",
                table: "SEARCHLOGS",
                column: "ResultCount");

            migrationBuilder.CreateIndex(
                name: "IX_SEARCHLOGS_SearchedAt",
                table: "SEARCHLOGS",
                column: "SearchedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SEARCHLOGS");
        }
    }
}
