namespace MIC.risk.DTOs;

public record CountByLabelDto(
    string Label,
    int Count
);

public record EarlyWarningIndicatorsDto(
    int CriticalResidualRisks,
    int WeakControls,
    int PendingReview
);

public record DepartmentMaturityDto(
    string DepartmentName,
    double MaturityScore
);

public record RiskMatrixCellDto(
    int Severity,
    int Frequency,
    int Count
);

/// <summary>Open risks plotted on the 5x5 severity-by-frequency grid, before controls.</summary>
public record InherentRiskMatrixDto(
    IEnumerable<RiskMatrixCellDto> Cells
);

/// <summary>Open risks counted per residual band, so the effect of controls is visible.</summary>
public record ResidualRiskBandDto(
    string Band,
    int Count
);

public record AnalyticsDashboardDto(
    double RiskAwarenessPercentage,
    EarlyWarningIndicatorsDto EarlyWarningIndicators,
    RiskActionSummaryDto OutstandingActions,
    double? AverageRiskResolutionTimeHours,
    int RisksSubmittedThisWeek,
    int RisksSubmittedThisMonth,
    IEnumerable<CountByLabelDto> RisksByDepartment,
    IEnumerable<CountByLabelDto> RisksByLocation,
    IEnumerable<CountByLabelDto> RiskSubcategoryDistribution,
    IEnumerable<DepartmentMaturityDto> RiskMaturityByDepartment,
    InherentRiskMatrixDto InherentRiskMatrix,
    IEnumerable<ResidualRiskBandDto> ResidualRiskBands
);

public record EmployeeDepartmentStatsDto(
    long DepartmentId,
    string DepartmentName,
    int ActiveEmployees,
    int EmployeesWithQuizCompletion,
    double AwarenessPercentage,
    int RiskReportCount
);
