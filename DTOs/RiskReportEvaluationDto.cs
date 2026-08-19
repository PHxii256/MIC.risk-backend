using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MIC.risk.DTOs
{
    public record CreateEvaluationRequestDto(
    int Severity,
    int Frequency,
    int ControlEffectiveness,
    string? ExistingMeasures,
    string? ProposedMeasures,
    int Priority
    );

    public record RiskReportEvaluationResponseDto(
        long Id,
        EmployeeResponseDto Evaluator,
        int Severity,
        int Frequency,
        int ControlEffectiveness,
        int InherentRisk,
        int ResidualRisk,
        string? ExistingMeasures,
        string? ProposedMeasures,
        int Priority,
        DateTimeOffset EvaluatedAt
    );

}