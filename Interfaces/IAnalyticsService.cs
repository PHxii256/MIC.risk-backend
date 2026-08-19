using MIC.risk.DTOs;

namespace MIC.risk.Services.Interfaces;

public interface IAnalyticsService
{
    Task<AnalyticsDashboardDto> GetDashboardAsync(DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<EmployeeDepartmentStatsDto>> GetEmployeeStatsByDepartmentAsync(CancellationToken cancellationToken = default);
}
