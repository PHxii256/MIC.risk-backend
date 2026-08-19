using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MIC.risk.Migrations
{
    /// <inheritdoc />
    public partial class SeedRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "3344651e-4661-469e-8949-42832c22eda4", "f9defef0-14fa-4277-80cc-28e38d1fb550", "User", "USER" },
                    { "7b28900d-0f15-48c6-aca6-8e0b9d7674ec", "b772b0d8-63aa-4661-bc20-77004debfdfb", "Admin", "ADMIN" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "3344651e-4661-469e-8949-42832c22eda4");

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: "7b28900d-0f15-48c6-aca6-8e0b9d7674ec");
        }
    }
}
