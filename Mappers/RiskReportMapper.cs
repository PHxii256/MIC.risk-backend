using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using MIC.risk.DTOs;
using MIC.risk.Models;

namespace MIC.risk.Mappers
{
    public static class RiskReportMapper
    {
        public static RiskReportResponseDto ToDto(this RiskReport report)
        {
            return new RiskReportResponseDto(
                report.Id,
                report.Employee != null ? report.Employee.ToDto() : null!,
                report.SubCategory != null ? report.SubCategory.ToDto() : null!,
                report.ReportedEvaluation != null ? report.ReportedEvaluation.ToDto() : null!,
                report.AuditorEvaluation?.ToDto(),
                report.Description,
                report.Status,
                report.SubmittedAt
            );
        }

        public static RiskReport ToEntity(this CreateRiskReportRequestDto dto, long reportedEvaluationId)
        {
            return new RiskReport
            {
                EmpId = dto.EmpId,
                SubCategoryId = dto.SubCategoryId,
                ReportedEvaluationId = reportedEvaluationId,
                Description = dto.Description,
                Status = "Submitted"
            };
        }

        public static RiskReportStatusHistoryResponseDto ToDto(this RiskReportStatusHistory history)
        {
            return new RiskReportStatusHistoryResponseDto(
                history.Id,
                history.ReportId,
                history.ChangedByEmployee != null ? history.ChangedByEmployee.ToDto() : null!,
                history.OldStatus,
                history.NewStatus,
                history.ChangedAt
            );
        }
    }
}