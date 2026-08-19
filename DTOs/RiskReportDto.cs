using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MIC.risk.DTOs
{
    public record CreateRiskReportRequestDto(
        long EmpId,
        long SubCategoryId,
        CreateEvaluationRequestDto Evaluation,
        string Description
    );

    public record UpdateRiskReportStatusRequestDto(
        string NewStatus
    );

    public record RiskReportResponseDto(
        long Id,
        EmployeeResponseDto Reporter,
        RiskSubcategoryResponseDto SubCategory,
        RiskReportEvaluationResponseDto ReportedEvaluation,
        RiskReportEvaluationResponseDto? AuditorEvaluation,
        string Description,
        string Status,
        DateTimeOffset SubmittedAt
    );
}