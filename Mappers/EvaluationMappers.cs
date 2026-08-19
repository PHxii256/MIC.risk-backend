using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MIC.risk.Models;
using MIC.risk.DTOs;

namespace MIC.risk.Mappers
{
    public static class EvaluationMappers
    {
        public static RiskReportEvaluationResponseDto ToDto(this RiskReportEvaluation evaluation)
        {
            return new RiskReportEvaluationResponseDto(
                evaluation.Id,
                evaluation.Employee != null ? evaluation.Employee.ToDto() : null!,
                evaluation.Severity,
                evaluation.Frequency,
                evaluation.ControlEffectiveness,
                evaluation.InherentRisk,
                evaluation.ResidualRisk,
                evaluation.ExistingMeasures,
                evaluation.ProposedMeasures,
                evaluation.Priority,
                evaluation.EvaluatedAt
            );
        }

        public static RiskReportEvaluation ToEntity(this CreateEvaluationRequestDto dto, long empId)
        {
            return new RiskReportEvaluation
            {
                EmpId = empId,
                Severity = dto.Severity,
                Frequency = dto.Frequency,
                ControlEffectiveness = dto.ControlEffectiveness,
                ExistingMeasures = dto.ExistingMeasures,
                ProposedMeasures = dto.ProposedMeasures,
                Priority = dto.Priority
            };
        }
    }
}