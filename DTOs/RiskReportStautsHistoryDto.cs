using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MIC.risk.DTOs
{
    public record RiskReportStatusHistoryResponseDto(
        long Id,
        long ReportId,
        EmployeeResponseDto ChangedBy,
        string OldStatus,
        string NewStatus,
        DateTimeOffset ChangedAt
    );
}