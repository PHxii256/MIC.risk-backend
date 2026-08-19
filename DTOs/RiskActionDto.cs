namespace MIC.risk.DTOs;

public record CreateRiskActionRequestDto(
    long ReportId,
    string Title,
    string? Description,
    long AssigneeEmpId,
    DateTimeOffset DueDate
);

public record UpdateRiskActionRequestDto(
    string Title,
    string? Description,
    long AssigneeEmpId,
    DateTimeOffset DueDate,
    string Status
);

public record RiskActionResponseDto(
    long Id,
    long ReportId,
    string Title,
    string? Description,
    EmployeeResponseDto Assignee,
    DateTimeOffset DueDate,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt
);

public record RiskActionSummaryDto(
    int OverdueCount,
    int DueThisWeekCount
);
