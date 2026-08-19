using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MIC.risk.Migrations
{
    /// <inheritdoc />
    public partial class risksubcategoriesaddedwithcheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE RiskSubCategory
                SET Category = 'Financial'
                WHERE Category NOT IN ('Financial', 'Operational', 'Strategic', 'Insurance');
            ");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RiskCategoryName",
                table: "RiskSubCategory",
                sql: "[Category] IN ('Financial', 'Operational', 'Strategic', 'Insurance')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RiskReportStatusHistory_NewStatus",
                table: "RiskReportStatusHistory",
                sql: "[NewStatus] IN ('Submitted', 'InReview', 'Resolved')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RiskReportStatusHistory_OldStatus",
                table: "RiskReportStatusHistory",
                sql: "[OldStatus] IN ('Submitted', 'InReview', 'Resolved')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_RiskCategoryName",
                table: "RiskSubCategory");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RiskReportStatusHistory_NewStatus",
                table: "RiskReportStatusHistory");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RiskReportStatusHistory_OldStatus",
                table: "RiskReportStatusHistory");
        }
    }
}
