using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MIC.risk.DTOs
{
    public record RecordResourceEngagementRequestDto(
        long EmpId,
        long ResourceId,
        bool Viewed,
        bool? SurveyCompleted
    );

    public record ResourceEngagementResponseDto(
        long Id,
        EmployeeResponseDto Employee,
        ResourceResponseDto Resource,
        bool Viewed,
        bool? SurveyCompleted,
        DateTimeOffset? ViewedAt,
        DateTimeOffset? CompletedAt
    );

    /// <summary>
    /// Engagement for one resource, counted over the people it is actually aimed at.
    /// Administrators curate the library rather than consume it, so they are excluded from both
    /// the count and the denominator — otherwise every resource would look permanently
    /// under-read by however many admins exist.
    /// </summary>
    public record ResourceEngagementStatsDto(
        long ResourceId,
        string ResourceName,
        string ResourceType,
        int ViewCount,
        int QuizCompletionCount,
        double CompletionRate,
        int EligibleEmployees
    );

    public record DepartmentEngagementStatsDto(
        long DepartmentId,
        string DepartmentName,
        int ActiveEmployees,
        int EmployeesWithQuizCompletion,
        double AwarenessPercentage
    );
}