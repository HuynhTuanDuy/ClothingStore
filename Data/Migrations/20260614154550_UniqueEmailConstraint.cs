using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClothingStore.Data.Migrations
{
    /// <inheritdoc />
    public partial class UniqueEmailConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT * FROM sys.objects WHERE name = 'UQ__ACCOUNTS__A9D105344376EEBF' AND type = 'UQ')
BEGIN
    ALTER TABLE ACCOUNTS DROP CONSTRAINT UQ__ACCOUNTS__A9D105344376EEBF
END
");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "ACCOUNTS",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_ACCOUNTS_Email",
                table: "ACCOUNTS",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ACCOUNTS_Email",
                table: "ACCOUNTS");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "ACCOUNTS",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
