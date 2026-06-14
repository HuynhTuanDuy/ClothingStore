using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClothingStore.Migrations
{
    /// <inheritdoc />
    public partial class ShipperUATFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActionType",
                table: "ORDERSTATUSHISTORY",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "AssignedAt",
                table: "ORDERS",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AssignedShipperId",
                table: "ORDERS",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeliveredAt",
                table: "ORDERS",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryFailureReason",
                table: "ORDERS",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryFailureReasonCode",
                table: "ORDERS",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ShippingStartedAt",
                table: "ORDERS",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ORDERS_AssignedShipperId",
                table: "ORDERS",
                column: "AssignedShipperId");

            migrationBuilder.AddForeignKey(
                name: "FK_ORDERS_ACCOUNTS_AssignedShipperId",
                table: "ORDERS",
                column: "AssignedShipperId",
                principalTable: "ACCOUNTS",
                principalColumn: "UserId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ORDERS_ACCOUNTS_AssignedShipperId",
                table: "ORDERS");

            migrationBuilder.DropIndex(
                name: "IX_ORDERS_AssignedShipperId",
                table: "ORDERS");

            migrationBuilder.DropColumn(
                name: "ActionType",
                table: "ORDERSTATUSHISTORY");

            migrationBuilder.DropColumn(
                name: "AssignedAt",
                table: "ORDERS");

            migrationBuilder.DropColumn(
                name: "AssignedShipperId",
                table: "ORDERS");

            migrationBuilder.DropColumn(
                name: "DeliveredAt",
                table: "ORDERS");

            migrationBuilder.DropColumn(
                name: "DeliveryFailureReason",
                table: "ORDERS");

            migrationBuilder.DropColumn(
                name: "DeliveryFailureReasonCode",
                table: "ORDERS");

            migrationBuilder.DropColumn(
                name: "ShippingStartedAt",
                table: "ORDERS");
        }
    }
}
