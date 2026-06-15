using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClothingStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscountProgramAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "DISCOUNTPROGRAMS",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DISCOUNTPROGRAMAUDITS",
                columns: table => new
                {
                    AuditID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProgramID = table.Column<int>(type: "int", nullable: false),
                    ActionType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedByUserId = table.Column<int>(type: "int", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DiscountProgramProgramID = table.Column<int>(type: "int", nullable: false),
                    ChangedByAccountUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DISCOUNTPROGRAMAUDITS", x => x.AuditID);
                    table.ForeignKey(
                        name: "FK_DISCOUNTPROGRAMAUDITS_ACCOUNTS_ChangedByAccountUserId",
                        column: x => x.ChangedByAccountUserId,
                        principalTable: "ACCOUNTS",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DISCOUNTPROGRAMAUDITS_DISCOUNTPROGRAMS_DiscountProgramProgramID",
                        column: x => x.DiscountProgramProgramID,
                        principalTable: "DISCOUNTPROGRAMS",
                        principalColumn: "ProgramID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DISCOUNTPROGRAMAUDITS_ChangedByAccountUserId",
                table: "DISCOUNTPROGRAMAUDITS",
                column: "ChangedByAccountUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DISCOUNTPROGRAMAUDITS_DiscountProgramProgramID",
                table: "DISCOUNTPROGRAMAUDITS",
                column: "DiscountProgramProgramID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DISCOUNTPROGRAMAUDITS");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "DISCOUNTPROGRAMS");
        }
    }
}
