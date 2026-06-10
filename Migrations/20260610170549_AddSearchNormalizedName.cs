using System;
using Microsoft.EntityFrameworkCore.Migrations;
using System.Text;

#nullable disable

namespace ClothingStore.Migrations
{
    public partial class AddSearchNormalizedName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SearchNormalizedName",
                table: "PRODUCTS",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_SearchNormalizedName",
                table: "PRODUCTS",
                column: "SearchNormalizedName");

            // Data Backfill
            string[] vietnameseSigns = new string[]
            {
                "aAeEoOuUiIdDyY",
                "áàạảãâấầậẩẫăắằặẳẵ",
                "ÁÀẠẢÃÂẤẦẬẨẪĂẮẰẶẲẴ",
                "éèẹẻẽêếềệểễ",
                "ÉÈẸẺẼÊẾỀỆỂỄ",
                "óòọỏõôốồộổỗơớờợởỡ",
                "ÓÒỌỎÕÔỐỒỘỔỖƠỚỜỢỞỠ",
                "úùụủũưứừựửữ",
                "ÚÙỤỦŨƯỨỪỰỬỮ",
                "íìịỉĩ",
                "ÍÌỊỈĨ",
                "đ",
                "Đ",
                "ýỳỵỷỹ",
                "ÝỲỴỶỸ"
            };

            var sqlBuilder = new StringBuilder();
            sqlBuilder.AppendLine("UPDATE PRODUCTS SET SearchNormalizedName = LOWER(ProductName);");

            for (int i = 1; i < vietnameseSigns.Length; i++)
            {
                char toChar = vietnameseSigns[0][i - 1];
                foreach (char fromChar in vietnameseSigns[i])
                {
                    sqlBuilder.AppendLine($"UPDATE PRODUCTS SET SearchNormalizedName = REPLACE(SearchNormalizedName, N'{fromChar}', N'{toChar}');");
                }
            }

            migrationBuilder.Sql(sqlBuilder.ToString());
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_SearchNormalizedName",
                table: "PRODUCTS");

            migrationBuilder.DropColumn(
                name: "SearchNormalizedName",
                table: "PRODUCTS");
        }
    }
}
