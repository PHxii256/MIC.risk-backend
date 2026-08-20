using MIC.risk.Domain;
using MIC.risk.DTOs;

namespace MIC.risk.Validation;

public static class RiskValidators
{
    public static readonly HashSet<string> ValidStatuses = new(StringComparer.Ordinal)
    {
        "Submitted", "InReview", "Resolved", "Archived"
    };

    public static void ValidateEvaluation(CreateEvaluationRequestDto dto)
    {
        ValidateRating(dto.Severity, nameof(dto.Severity));
        ValidateRating(dto.Frequency, nameof(dto.Frequency));
        ValidateRating(dto.ControlEffectiveness, nameof(dto.ControlEffectiveness));
        ValidateRating(dto.Priority, nameof(dto.Priority));
    }

    private static void ValidateRating(int value, string fieldName)
    {
        if (value < RiskScoring.MinRating || value > RiskScoring.MaxRating)
        {
            throw new InvalidOperationException(
                $"{fieldName} must be between {RiskScoring.MinRating} and {RiskScoring.MaxRating}.");
        }
    }

    public static void ValidateStatus(string status)
    {
        if (!ValidStatuses.Contains(status))
        {
            throw new InvalidOperationException($"Status must be one of: {string.Join(", ", ValidStatuses)}.");
        }
    }

    public static void ValidateStatusTransition(string currentStatus, string newStatus)
    {
        ValidateStatus(currentStatus);
        ValidateStatus(newStatus);
    }
}
