using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MIC.risk.Migrations
{
    /// <inheritdoc />
    public partial class RefreshTokensAndRiskScoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_RiskReportEvaluation_MeasuresEffectiveness",
                table: "RiskReportEvaluation");

            migrationBuilder.DropColumn(
                name: "RiskScore",
                table: "RiskReportEvaluation");

            migrationBuilder.RenameColumn(
                name: "MeasuresEffectiveness",
                table: "RiskReportEvaluation",
                newName: "ControlEffectiveness");

            migrationBuilder.AddColumn<int>(
                name: "InherentRisk",
                table: "RiskReportEvaluation",
                type: "int",
                nullable: false,
                computedColumnSql: "[Severity] * [Frequency]",
                stored: true);

            migrationBuilder.AddColumn<int>(
                name: "ResidualRisk",
                table: "RiskReportEvaluation",
                type: "int",
                nullable: false,
                computedColumnSql: "[Severity] * [Frequency] * [ControlEffectiveness]",
                stored: true);

            migrationBuilder.CreateTable(
                name: "RefreshToken",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TokenHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    FamilyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FamilyExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    RevokedReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ReplacedByTokenId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedByIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshToken", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshToken_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_RiskReportEvaluation_ControlEffectiveness",
                table: "RiskReportEvaluation",
                sql: "[ControlEffectiveness] BETWEEN 1 AND 5");

            // Priority was previously unbounded, so existing rows may sit outside 1-5 and would
            // make the new constraint fail to apply. Clamp them into range first.
            migrationBuilder.Sql(
                "UPDATE [RiskReportEvaluation] SET [Priority] = 1 WHERE [Priority] < 1;");
            migrationBuilder.Sql(
                "UPDATE [RiskReportEvaluation] SET [Priority] = 5 WHERE [Priority] > 5;");

            migrationBuilder.AddCheckConstraint(
                name: "CK_RiskReportEvaluation_Priority",
                table: "RiskReportEvaluation",
                sql: "[Priority] BETWEEN 1 AND 5");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_FamilyId",
                table: "RefreshToken",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_TokenHash",
                table: "RefreshToken",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshToken_UserId",
                table: "RefreshToken",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefreshToken");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RiskReportEvaluation_ControlEffectiveness",
                table: "RiskReportEvaluation");

            migrationBuilder.DropCheckConstraint(
                name: "CK_RiskReportEvaluation_Priority",
                table: "RiskReportEvaluation");

            migrationBuilder.DropColumn(
                name: "InherentRisk",
                table: "RiskReportEvaluation");

            migrationBuilder.DropColumn(
                name: "ResidualRisk",
                table: "RiskReportEvaluation");

            migrationBuilder.RenameColumn(
                name: "ControlEffectiveness",
                table: "RiskReportEvaluation",
                newName: "MeasuresEffectiveness");

            migrationBuilder.AddColumn<int>(
                name: "RiskScore",
                table: "RiskReportEvaluation",
                type: "int",
                nullable: false,
                computedColumnSql: "[severity] * [frequency]",
                stored: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_RiskReportEvaluation_MeasuresEffectiveness",
                table: "RiskReportEvaluation",
                sql: "[MeasuresEffectiveness] BETWEEN 1 AND 5");
        }
    }
}
