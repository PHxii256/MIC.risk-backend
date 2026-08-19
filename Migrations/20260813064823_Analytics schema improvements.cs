using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MIC.risk.Migrations
{
    /// <inheritdoc />
    public partial class Analyticsschemaimprovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ResolvedAt",
                table: "RiskReport",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_RiskReportEvaluation_Frequency",
                table: "RiskReportEvaluation",
                sql: "[Frequency] BETWEEN 1 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RiskReportEvaluation_MeasuresEffectiveness",
                table: "RiskReportEvaluation",
                sql: "[MeasuresEffectiveness] BETWEEN 1 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RiskReportEvaluation_Severity",
                table: "RiskReportEvaluation",
                sql: "[Severity] BETWEEN 1 AND 5");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RiskReport_Status",
                table: "RiskReport",
                sql: "[Status] IN ('Submitted', 'InReview', 'Resolved')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_RiskReportEvaluation_Frequency",
                table: "RiskReportEvaluation");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RiskReportEvaluation_MeasuresEffectiveness",
                table: "RiskReportEvaluation");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RiskReportEvaluation_Severity",
                table: "RiskReportEvaluation");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RiskReport_Status",
                table: "RiskReport");

            migrationBuilder.DropColumn(
                name: "ResolvedAt",
                table: "RiskReport");
        }
    }
}
