using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MIC.risk.Migrations
{
    /// <inheritdoc />
    public partial class Followupimprovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CompletedAt",
                table: "ResourceEngagement",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ViewedAt",
                table: "ResourceEngagement",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Active",
                table: "Resource",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "RiskAction",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportId = table.Column<long>(type: "bigint", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AssigneeEmpId = table.Column<long>(type: "bigint", nullable: false),
                    DueDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RiskAction", x => x.Id);
                    table.CheckConstraint("CK_RiskAction_Status", "[Status] IN ('Pending', 'Completed')");
                    table.ForeignKey(
                        name: "FK_RiskAction_Employee_AssigneeEmpId",
                        column: x => x.AssigneeEmpId,
                        principalTable: "Employee",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RiskAction_RiskReport_ReportId",
                        column: x => x.ReportId,
                        principalTable: "RiskReport",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RiskAction_AssigneeEmpId",
                table: "RiskAction",
                column: "AssigneeEmpId");

            migrationBuilder.CreateIndex(
                name: "IX_RiskAction_ReportId",
                table: "RiskAction",
                column: "ReportId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RiskAction");

            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "ResourceEngagement");

            migrationBuilder.DropColumn(
                name: "ViewedAt",
                table: "ResourceEngagement");

            migrationBuilder.DropColumn(
                name: "Active",
                table: "Resource");
        }
    }
}
