using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClothingStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderIndexesForRecommendation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId_OrderDate",
                table: "ORDERS",
                columns: new[] { "CustomerId", "OrderDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Orders_ShippingAddressId",
                table: "ORDERS",
                column: "ShippingAddressId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_CustomerId_OrderDate",
                table: "ORDERS");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ShippingAddressId",
                table: "ORDERS");
        }
    }
}
