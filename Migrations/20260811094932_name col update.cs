using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MIC.risk.Migrations
{
    /// <inheritdoc />
    public partial class namecolupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Name",
                table: "ResourceType",
                newName: "[Name]");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Resource",
                newName: "[Name]");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Employee",
                newName: "[Name]");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "Department",
                newName: "[Name]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "[Name]",
                table: "ResourceType",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "[Name]",
                table: "Resource",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "[Name]",
                table: "Employee",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "[Name]",
                table: "Department",
                newName: "Name");
        }
    }
}
