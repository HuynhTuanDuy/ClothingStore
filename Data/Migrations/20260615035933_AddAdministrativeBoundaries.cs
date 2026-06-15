using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClothingStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdministrativeBoundaries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DistrictId",
                table: "SHIPPINGADDRESSES",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "SHIPPINGADDRESSES",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProvinceId",
                table: "SHIPPINGADDRESSES",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WardId",
                table: "SHIPPINGADDRESSES",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryNote",
                table: "ORDERS",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingFullAddress",
                table: "ORDERS",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "PROVINCES",
                columns: table => new
                {
                    ProvinceId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PROVINCES", x => x.ProvinceId);
                });

            migrationBuilder.CreateTable(
                name: "DISTRICTS",
                columns: table => new
                {
                    DistrictId = table.Column<int>(type: "int", nullable: false),
                    ProvinceId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DISTRICTS", x => x.DistrictId);
                    table.ForeignKey(
                        name: "FK_DISTRICTS_PROVINCES_ProvinceId",
                        column: x => x.ProvinceId,
                        principalTable: "PROVINCES",
                        principalColumn: "ProvinceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WARDS",
                columns: table => new
                {
                    WardId = table.Column<int>(type: "int", nullable: false),
                    DistrictId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WARDS", x => x.WardId);
                    table.ForeignKey(
                        name: "FK_WARDS_DISTRICTS_DistrictId",
                        column: x => x.DistrictId,
                        principalTable: "DISTRICTS",
                        principalColumn: "DistrictId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SHIPPINGADDRESSES_DistrictId",
                table: "SHIPPINGADDRESSES",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_SHIPPINGADDRESSES_ProvinceId",
                table: "SHIPPINGADDRESSES",
                column: "ProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_SHIPPINGADDRESSES_WardId",
                table: "SHIPPINGADDRESSES",
                column: "WardId");

            migrationBuilder.CreateIndex(
                name: "IX_DISTRICTS_ProvinceId",
                table: "DISTRICTS",
                column: "ProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_WARDS_DistrictId",
                table: "WARDS",
                column: "DistrictId");

            migrationBuilder.AddForeignKey(
                name: "FK_SHIPPINGADDRESSES_DISTRICTS_DistrictId",
                table: "SHIPPINGADDRESSES",
                column: "DistrictId",
                principalTable: "DISTRICTS",
                principalColumn: "DistrictId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SHIPPINGADDRESSES_PROVINCES_ProvinceId",
                table: "SHIPPINGADDRESSES",
                column: "ProvinceId",
                principalTable: "PROVINCES",
                principalColumn: "ProvinceId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SHIPPINGADDRESSES_WARDS_WardId",
                table: "SHIPPINGADDRESSES",
                column: "WardId",
                principalTable: "WARDS",
                principalColumn: "WardId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SHIPPINGADDRESSES_DISTRICTS_DistrictId",
                table: "SHIPPINGADDRESSES");

            migrationBuilder.DropForeignKey(
                name: "FK_SHIPPINGADDRESSES_PROVINCES_ProvinceId",
                table: "SHIPPINGADDRESSES");

            migrationBuilder.DropForeignKey(
                name: "FK_SHIPPINGADDRESSES_WARDS_WardId",
                table: "SHIPPINGADDRESSES");

            migrationBuilder.DropTable(
                name: "WARDS");

            migrationBuilder.DropTable(
                name: "DISTRICTS");

            migrationBuilder.DropTable(
                name: "PROVINCES");

            migrationBuilder.DropIndex(
                name: "IX_SHIPPINGADDRESSES_DistrictId",
                table: "SHIPPINGADDRESSES");

            migrationBuilder.DropIndex(
                name: "IX_SHIPPINGADDRESSES_ProvinceId",
                table: "SHIPPINGADDRESSES");

            migrationBuilder.DropIndex(
                name: "IX_SHIPPINGADDRESSES_WardId",
                table: "SHIPPINGADDRESSES");

            migrationBuilder.DropColumn(
                name: "DistrictId",
                table: "SHIPPINGADDRESSES");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "SHIPPINGADDRESSES");

            migrationBuilder.DropColumn(
                name: "ProvinceId",
                table: "SHIPPINGADDRESSES");

            migrationBuilder.DropColumn(
                name: "WardId",
                table: "SHIPPINGADDRESSES");

            migrationBuilder.DropColumn(
                name: "DeliveryNote",
                table: "ORDERS");

            migrationBuilder.DropColumn(
                name: "ShippingFullAddress",
                table: "ORDERS");
        }
    }
}
