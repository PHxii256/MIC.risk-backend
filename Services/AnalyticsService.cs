using Microsoft.EntityFrameworkCore;
using MIC.risk.Data;
using MIC.risk.Domain;
using MIC.risk.DTOs;
using MIC.risk.Models;
using MIC.risk.Services.Interfaces;

namespace MIC.risk.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly ApplicationDBContext _context;

    public AnalyticsService(ApplicationDBContext context)
    {
        _context = context;
    }

    public async Task<AnalyticsDashboardDto> GetDashboardAsync(
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var weekStart = now.AddDays(-(int)now.DayOfWeek);
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

        var activeEmployees = await _context.Employees
            .AsNoTracking()
            .Where(e => e.Active)
            .ToListAsync(cancellationToken);

        var quizResourceIds = await _context.Resources.AsNoTracking()
            .Where(r => r.Type == "Quiz" && r.Active)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var employeesWithQuizCompletion = await _context.ResourceEngagements
            .AsNoTracking()
            .Where(re => quizResourceIds.Contains(re.ResourceId) && re.SurveyCompleted == true)
            .Select(re => re.EmpId)
            .Distinct()
            .CountAsync(cancellationToken);

        var awarenessPercentage = activeEmployees.Count == 0
            ? 0
            : Math.Round((double)employeesWithQuizCompletion / activeEmployees.Count * 100, 2);

        var reportsQuery = _context.RiskReports
            .AsNoTracking()
            .Include(r => r.Employee).ThenInclude(e => e.Department)
            .Include(r => r.SubCategory)
            .Include(r => r.ReportedEvaluation)
            .Include(r => r.AuditorEvaluation)
            .AsQueryable();

        if (from.HasValue)
        {
            reportsQuery = reportsQuery.Where(r => r.SubmittedAt >= from.Value);
        }

        if (to.HasValue)
        {
            reportsQuery = reportsQuery.Where(r => r.SubmittedAt <= to.Value);
        }

        var reports = await reportsQuery.ToListAsync(cancellationToken);

        var openReports = reports
            .Where(r => r.Status != "Resolved" && r.Status != "Archived")
            .ToList();

        var earlyWarning = new EarlyWarningIndicatorsDto(
            CriticalResidualRisks: openReports.Count(r => GetResidualRisk(r) >= RiskScoring.CriticalBandFloor),
            WeakControls: openReports.Count(r => GetControlEffectiveness(r) >= RiskScoring.WeakControlThreshold),
            PendingReview: reports.Count(r => r.Status == "InReview")
        );

        var resolvedReports = await _context.RiskReports
            .AsNoTracking()
            .Where(r => r.ResolvedAt != null)
            .ToListAsync(cancellationToken);

        double? averageResolutionHours = resolvedReports.Count == 0
            ? null
            : Math.Round(resolvedReports
                .Average(r => (r.ResolvedAt!.Value - r.SubmittedAt).TotalHours), 2);

        var allReportsForCounts = await _context.RiskReports.AsNoTracking().ToListAsync(cancellationToken);

        var risksThisWeek = allReportsForCounts.Count(r => r.SubmittedAt >= weekStart);
        var risksThisMonth = allReportsForCounts.Count(r => r.SubmittedAt >= monthStart);

        var risksByDepartment = reports
            .GroupBy(r => r.Employee.Department.Name)
            .Select(g => new CountByLabelDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        var risksByLocation = reports
            .GroupBy(r => r.Employee.Department.BranchLocation)
            .Select(g => new CountByLabelDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        var riskSubcategoryDistribution = reports
            .Where(r => r.SubCategory.Active)
            .GroupBy(r => r.SubCategory.NameEn)
            .Select(g => new CountByLabelDto(g.Key, g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();

        var departments = await _context.Departments.AsNoTracking().ToListAsync(cancellationToken);
        var completedEmployeeIds = await _context.ResourceEngagements.AsNoTracking()
            .Where(re => quizResourceIds.Contains(re.ResourceId) && re.SurveyCompleted == true)
            .Select(re => re.EmpId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var maturityByDepartment = departments.Select(department =>
        {
            var deptEmployees = activeEmployees.Where(e => e.DeptId == department.Id).ToList();
            var activeCount = deptEmployees.Count;
            var completedCount = deptEmployees.Count(e => completedEmployeeIds.Contains(e.Id));
            var score = activeCount == 0 ? 0 : Math.Round((double)completedCount / activeCount * 100, 2);

            return new DepartmentMaturityDto(department.Name, score);
        }).ToList();

        var matrixCells = openReports
            .Select(r =>
            {
                var evaluation = GetEffectiveEvaluation(r);
                return new { evaluation.Severity, evaluation.Frequency };
            })
            .GroupBy(x => new { x.Severity, x.Frequency })
            .Select(g => new RiskMatrixCellDto(g.Key.Severity, g.Key.Frequency, g.Count()))
            .OrderBy(c => c.Severity)
            .ThenBy(c => c.Frequency)
            .ToList();

        // Every band is present even at zero, so the client can render a stable set of bars.
        var residualBands = Enum.GetValues<RiskBand>()
            .Select(band => new ResidualRiskBandDto(
                band.ToString(),
                openReports.Count(r => RiskScoring.Band(GetResidualRisk(r)) == band)))
            .ToList();

        var nowUtc = DateTimeOffset.UtcNow;
        var weekEnd = nowUtc.AddDays(7);
        var pendingActions = await _context.RiskActions.AsNoTracking()
            .Where(a => a.Status == "Pending")
            .ToListAsync(cancellationToken);
        var outstandingActions = new RiskActionSummaryDto(
            pendingActions.Count(a => a.DueDate < nowUtc),
            pendingActions.Count(a => a.DueDate >= nowUtc && a.DueDate <= weekEnd));

        return new AnalyticsDashboardDto(
            awarenessPercentage,
            earlyWarning,
            outstandingActions,
            averageResolutionHours,
            risksThisWeek,
            risksThisMonth,
            risksByDepartment,
            risksByLocation,
            riskSubcategoryDistribution,
            maturityByDepartment,
            new InherentRiskMatrixDto(matrixCells),
            residualBands
        );
    }

    public async Task<IEnumerable<EmployeeDepartmentStatsDto>> GetEmployeeStatsByDepartmentAsync(
        CancellationToken cancellationToken = default)
    {
        var departments = await _context.Departments.AsNoTracking().ToListAsync(cancellationToken);
        var activeEmployees = await _context.Employees.AsNoTracking().Where(e => e.Active).ToListAsync(cancellationToken);

        var quizResourceIds = await _context.Resources.AsNoTracking()
            .Where(r => r.Type == "Quiz" && r.Active)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var completedEmployeeIds = await _context.ResourceEngagements.AsNoTracking()
            .Where(re => quizResourceIds.Contains(re.ResourceId) && re.SurveyCompleted == true)
            .Select(re => re.EmpId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var reportCounts = await _context.RiskReports.AsNoTracking()
            .GroupBy(r => r.EmpId)
            .Select(g => new { EmpId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return departments.Select(department =>
        {
            var deptEmployees = activeEmployees.Where(e => e.DeptId == department.Id).ToList();
            var activeCount = deptEmployees.Count;
            var completedCount = deptEmployees.Count(e => completedEmployeeIds.Contains(e.Id));
            var awareness = activeCount == 0 ? 0 : Math.Round((double)completedCount / activeCount * 100, 2);
            var riskCount = deptEmployees.Sum(e => reportCounts.FirstOrDefault(rc => rc.EmpId == e.Id)?.Count ?? 0);

            return new EmployeeDepartmentStatsDto(
                department.Id,
                department.Name,
                activeCount,
                completedCount,
                awareness,
                riskCount);
        });
    }

    private static RiskReportEvaluation GetEffectiveEvaluation(RiskReport report) =>
        report.AuditorEvaluation ?? report.ReportedEvaluation;

    private static int GetResidualRisk(RiskReport report) =>
        GetEffectiveEvaluation(report).ResidualRisk;

    private static int GetControlEffectiveness(RiskReport report) =>
        GetEffectiveEvaluation(report).ControlEffectiveness;
}
