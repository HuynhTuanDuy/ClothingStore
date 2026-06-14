using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClothingStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDeliveryAttemptInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DeliveryAttemptCount",
                table: "ORDERS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DeliveryRescheduleReason",
                table: "ORDERS",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastDeliveryAttemptAt",
                table: "ORDERS",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextDeliveryDate",
                table: "ORDERS",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeliveryAttemptCount",
                table: "ORDERS");

            migrationBuilder.DropColumn(
                name: "DeliveryRescheduleReason",
                table: "ORDERS");

            migrationBuilder.DropColumn(
                name: "LastDeliveryAttemptAt",
                table: "ORDERS");

            migrationBuilder.DropColumn(
                name: "NextDeliveryDate",
                table: "ORDERS");
        }
    }
}
