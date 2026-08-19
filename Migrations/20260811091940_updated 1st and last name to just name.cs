using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MIC.risk.Migrations
{
    /// <inheritdoc />
    public partial class updated1standlastnametojustname : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Employee");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "Employee",
                newName: "Name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Employee",
                newName: "LastName");

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Employee",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }
    }
}
