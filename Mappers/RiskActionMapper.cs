using MIC.risk.DTOs;
using MIC.risk.Models;

namespace MIC.risk.Mappers;

public static class RiskActionMapper
{
    public static RiskActionResponseDto ToDto(this RiskAction action)
    {
        return new RiskActionResponseDto(
            action.Id,
            action.ReportId,
            action.Title,
            action.Description,
            action.Assignee.ToDto(),
            action.DueDate,
            action.Status,
            action.CreatedAt,
            action.CompletedAt
        );
    }
}
