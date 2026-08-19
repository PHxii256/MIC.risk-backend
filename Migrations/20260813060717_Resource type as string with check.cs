using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MIC.risk.Migrations
{
    /// <inheritdoc />
    public partial class Resourcetypeasstringwithcheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Resource_ResourceType_ResourceTypeName",
                table: "Resource");

            migrationBuilder.DropTable(
                name: "ResourceType");

            migrationBuilder.DropIndex(
                name: "IX_Resource_ResourceTypeName",
                table: "Resource");

            migrationBuilder.DropColumn(
                name: "ResourceTypeName",
                table: "Resource");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "Resource",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Resource_Type",
                table: "Resource",
                sql: "[Type] IN ('Video', 'Image', 'File', 'Quiz', 'Link')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Resource_Type",
                table: "Resource");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Resource");

            migrationBuilder.AddColumn<string>(
                name: "ResourceTypeName",
                table: "Resource",
                type: "nvarchar(100)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ResourceType",
                columns: table => new
                {
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceType", x => x.Name);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Resource_ResourceTypeName",
                table: "Resource",
                column: "ResourceTypeName");

            migrationBuilder.AddForeignKey(
                name: "FK_Resource_ResourceType_ResourceTypeName",
                table: "Resource",
                column: "ResourceTypeName",
                principalTable: "ResourceType",
                principalColumn: "Name",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
