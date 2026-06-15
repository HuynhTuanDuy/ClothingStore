using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClothingStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAddressEnhancements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AddressName",
                table: "SHIPPINGADDRESSES",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Latitude",
                table: "SHIPPINGADDRESSES",
                type: "numeric(9,6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Longitude",
                table: "SHIPPINGADDRESSES",
                type: "numeric(9,6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShippingAddressId",
                table: "ORDERS",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AddressName",
                table: "SHIPPINGADDRESSES");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "SHIPPINGADDRESSES");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "SHIPPINGADDRESSES");

            migrationBuilder.DropColumn(
                name: "ShippingAddressId",
                table: "ORDERS");
        }
    }
}
