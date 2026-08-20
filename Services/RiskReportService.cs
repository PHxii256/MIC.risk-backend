using Microsoft.EntityFrameworkCore;
using MIC.risk.Data;
using MIC.risk.DTOs;
using MIC.risk.Mappers;
using MIC.risk.Models;
using MIC.risk.Services.Interfaces;
using MIC.risk.Validation;

namespace MIC.risk.Services;

public class RiskReportService : IRiskReportService
{
    private readonly ApplicationDBContext _context;

    public RiskReportService(ApplicationDBContext context)
    {
        _context = context;
    }

    public async Task<RiskReport?> GetEntityByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await _context.RiskReports
            .AsNoTracking()
            // Every employee on the wire is an EmployeeResponseDto, which the contract declares
            // with a required department and email. Both live on navigations, so an evaluation's
            // evaluator needs the same chain the report's reporter gets.
            .Include(r => r.Employee).ThenInclude(e => e.Department)
            .Include(r => r.Employee).ThenInclude(e => e.IdentityUser)
            .Include(r => r.SubCategory)
            .Include(r => r.ReportedEvaluation).ThenInclude(ev => ev.Employee).ThenInclude(e => e.Department)
            .Include(r => r.ReportedEvaluation).ThenInclude(ev => ev.Employee).ThenInclude(e => e.IdentityUser)
            .Include(r => r.AuditorEvaluation).ThenInclude(ev => ev!.Employee).ThenInclude(e => e.Department)
            .Include(r => r.AuditorEvaluation).ThenInclude(ev => ev!.Employee).ThenInclude(e => e.IdentityUser)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<RiskReportResponseDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var report = await GetEntityByIdAsync(id, cancellationToken);
        return report?.ToDto();
    }

    public async Task<PagedResultDto<RiskReportResponseDto>> GetAllAsync(
        string? status,
        int page,
        int pageSize,
        string? search = null,
        string? sortBy = null,
        string? sortDir = null,
        CancellationToken cancellationToken = default)
    {
        var (normalizedPage, normalizedPageSize) = PaginationHelper.Normalize(page, pageSize);

        var query = _context.RiskReports
            .AsNoTracking()
            // Every employee on the wire is an EmployeeResponseDto, which the contract declares
            // with a required department and email. Both live on navigations, so an evaluation's
            // evaluator needs the same chain the report's reporter gets.
            .Include(r => r.Employee).ThenInclude(e => e.Department)
            .Include(r => r.Employee).ThenInclude(e => e.IdentityUser)
            .Include(r => r.SubCategory)
            .Include(r => r.ReportedEvaluation).ThenInclude(ev => ev.Employee).ThenInclude(e => e.Department)
            .Include(r => r.ReportedEvaluation).ThenInclude(ev => ev.Employee).ThenInclude(e => e.IdentityUser)
            .Include(r => r.AuditorEvaluation).ThenInclude(ev => ev!.Employee).ThenInclude(e => e.Department)
            .Include(r => r.AuditorEvaluation).ThenInclude(ev => ev!.Employee).ThenInclude(e => e.IdentityUser)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            RiskValidators.ValidateStatus(status);
            query = query.Where(r => r.Status == status);
        }

        // Filtering runs in the database rather than over the current page, so a search reaches
        // every report rather than only the twenty already on screen.
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(r =>
                EF.Functions.Like(r.Description, $"%{term}%") ||
                EF.Functions.Like(r.Employee.Name, $"%{term}%") ||
                EF.Functions.Like(r.SubCategory.NameEn, $"%{term}%"));
        }

        query = ApplySort(query, sortBy, sortDir);

        var totalCount = await query.CountAsync(cancellationToken);
        var reports = await query
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<RiskReportResponseDto>(
            reports.Select(r => r.ToDto()),
            normalizedPage,
            normalizedPageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)normalizedPageSize));
    }

    public async Task<IEnumerable<RiskReportResponseDto>> GetByEmployeeIdAsync(long empId, CancellationToken cancellationToken = default)
    {
        var reports = await _context.RiskReports
            .AsNoTracking()
            // Every employee on the wire is an EmployeeResponseDto, which the contract declares
            // with a required department and email. Both live on navigations, so an evaluation's
            // evaluator needs the same chain the report's reporter gets.
            .Include(r => r.Employee).ThenInclude(e => e.Department)
            .Include(r => r.Employee).ThenInclude(e => e.IdentityUser)
            .Include(r => r.SubCategory)
            .Include(r => r.ReportedEvaluation).ThenInclude(ev => ev.Employee).ThenInclude(e => e.Department)
            .Include(r => r.ReportedEvaluation).ThenInclude(ev => ev.Employee).ThenInclude(e => e.IdentityUser)
            .Include(r => r.AuditorEvaluation).ThenInclude(ev => ev!.Employee).ThenInclude(e => e.Department)
            .Include(r => r.AuditorEvaluation).ThenInclude(ev => ev!.Employee).ThenInclude(e => e.IdentityUser)
            .Where(r => r.EmpId == empId)
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync(cancellationToken);

        return reports.Select(r => r.ToDto());
    }

    public async Task<RiskReportResponseDto> CreateReportAsync(CreateRiskReportRequestDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Description))
        {
            throw new InvalidOperationException("Report description is required.");
        }

        RiskValidators.ValidateEvaluation(dto.Evaluation);

        var subCategory = await _context.RiskSubCategories
            .FirstOrDefaultAsync(sc => sc.Id == dto.SubCategoryId && sc.Active, cancellationToken);
        if (subCategory == null)
        {
            throw new InvalidOperationException("Subcategory does not exist or is inactive.");
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var evaluationEntity = dto.Evaluation.ToEntity(dto.EmpId);
            _context.RiskReportEvaluations.Add(evaluationEntity);
            await _context.SaveChangesAsync(cancellationToken);

            var reportEntity = dto.ToEntity(evaluationEntity.Id);
            _context.RiskReports.Add(reportEntity);
            await _context.SaveChangesAsync(cancellationToken);

            var initialHistory = new RiskReportStatusHistory
            {
                ReportId = reportEntity.Id,
                ChangedBy = dto.EmpId,
                OldStatus = "Submitted",
                NewStatus = "Submitted",
                ChangedAt = reportEntity.SubmittedAt
            };
            _context.RiskReportStatusHistories.Add(initialHistory);
            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return (await GetByIdAsync(reportEntity.Id, cancellationToken))!;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <summary>
    /// Ordering happens in the database so it spans the whole result set, not just one page.
    /// Unknown keys fall back to newest-first rather than failing the request.
    /// </summary>
    private static IQueryable<RiskReport> ApplySort(IQueryable<RiskReport> query, string? sortBy, string? sortDir)
    {
        var descending = !string.Equals(sortDir, "asc", StringComparison.OrdinalIgnoreCase);

        return (sortBy?.ToLowerInvariant()) switch
        {
            "status" => descending ? query.OrderByDescending(r => r.Status) : query.OrderBy(r => r.Status),
            "reporter" => descending ? query.OrderByDescending(r => r.Employee.Name) : query.OrderBy(r => r.Employee.Name),
            "subcategory" => descending ? query.OrderByDescending(r => r.SubCategory.NameEn) : query.OrderBy(r => r.SubCategory.NameEn),
            "inherentrisk" => descending
                ? query.OrderByDescending(r => (r.AuditorEvaluation ?? r.ReportedEvaluation).InherentRisk)
                : query.OrderBy(r => (r.AuditorEvaluation ?? r.ReportedEvaluation).InherentRisk),
            "residualrisk" => descending
                ? query.OrderByDescending(r => (r.AuditorEvaluation ?? r.ReportedEvaluation).ResidualRisk)
                : query.OrderBy(r => (r.AuditorEvaluation ?? r.ReportedEvaluation).ResidualRisk),
            _ => descending ? query.OrderByDescending(r => r.SubmittedAt) : query.OrderBy(r => r.SubmittedAt),
        };
    }

    public async Task<RiskReportResponseDto?> UpdateAuditorEvaluationAsync(
        long reportId,
        CreateEvaluationRequestDto dto,
        long auditorEmpId,
        CancellationToken cancellationToken = default)
    {
        RiskValidators.ValidateEvaluation(dto);

        var report = await _context.RiskReports.FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);
        if (report == null) return null;

        if (!report.AuditorEvaluationId.HasValue)
        {
            throw new InvalidOperationException(
                "This report has no auditor evaluation yet. Create one before revising it.");
        }

        var evaluation = await _context.RiskReportEvaluations
            .FirstOrDefaultAsync(e => e.Id == report.AuditorEvaluationId.Value, cancellationToken);

        if (evaluation == null) return null;

        evaluation.Severity = dto.Severity;
        evaluation.Frequency = dto.Frequency;
        evaluation.ControlEffectiveness = dto.ControlEffectiveness;
        evaluation.ExistingMeasures = dto.ExistingMeasures;
        evaluation.ProposedMeasures = dto.ProposedMeasures;
        evaluation.Priority = dto.Priority;

        // Re-stamped on every revision: this is what "last modified by, and when" reads from.
        // The inherent and residual scores are computed columns, so they follow automatically.
        evaluation.EmpId = auditorEmpId;
        evaluation.EvaluatedAt = DateTimeOffset.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(reportId, cancellationToken);
    }

    public async Task<RiskReportResponseDto?> AttachAuditorEvaluationAsync(
        long reportId,
        CreateEvaluationRequestDto dto,
        long auditorEmpId,
        CancellationToken cancellationToken = default)
    {
        RiskValidators.ValidateEvaluation(dto);

        var report = await _context.RiskReports.FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);
        if (report == null) return null;

        if (report.AuditorEvaluationId.HasValue)
        {
            throw new InvalidOperationException("An auditor evaluation already exists for this report.");
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var evaluationEntity = dto.ToEntity(auditorEmpId);
            _context.RiskReportEvaluations.Add(evaluationEntity);
            await _context.SaveChangesAsync(cancellationToken);

            report.AuditorEvaluationId = evaluationEntity.Id;
            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return await GetByIdAsync(reportId, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<RiskReportResponseDto?> UpdateStatusAsync(
        long reportId,
        string newStatus,
        long changedByEmpId,
        CancellationToken cancellationToken = default)
    {
        var report = await _context.RiskReports.FirstOrDefaultAsync(r => r.Id == reportId, cancellationToken);
        if (report == null) return null;

        RiskValidators.ValidateStatusTransition(report.Status, newStatus);

        if (report.Status == newStatus)
        {
            return await GetByIdAsync(reportId, cancellationToken);
        }

        if (newStatus == "Resolved" &&
            !await _context.RiskActions.AnyAsync(a => a.ReportId == reportId, cancellationToken))
        {
            throw new InvalidOperationException(
                "Add at least one mitigation before resolving this risk report.");
        }

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var history = new RiskReportStatusHistory
            {
                ReportId = reportId,
                ChangedBy = changedByEmpId,
                OldStatus = report.Status,
                NewStatus = newStatus,
                ChangedAt = DateTimeOffset.UtcNow
            };

            _context.RiskReportStatusHistories.Add(history);

            report.Status = newStatus;
            if (newStatus == "Resolved")
            {
                report.ResolvedAt ??= DateTimeOffset.UtcNow;
            }
            else if (newStatus != "Archived")
            {
                // Archiving a resolved report must not erase its resolution timestamp. Moving
                // it back into an active workflow state does mean it is no longer resolved.
                report.ResolvedAt = null;
            }

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await GetByIdAsync(reportId, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<PagedResultDto<RiskReportStatusHistoryResponseDto>> GetStatusHistoryAsync(
        long reportId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (normalizedPage, normalizedPageSize) = PaginationHelper.Normalize(page, pageSize);

        var query = _context.RiskReportStatusHistories
            .AsNoTracking()
            .Include(h => h.ChangedByEmployee).ThenInclude(e => e.Department)
            .Include(h => h.ChangedByEmployee).ThenInclude(e => e.IdentityUser)
            .Where(h => h.ReportId == reportId)
            .OrderByDescending(h => h.ChangedAt);

        var totalCount = await query.CountAsync(cancellationToken);
        var histories = await query
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<RiskReportStatusHistoryResponseDto>(
            histories.Select(h => h.ToDto()),
            normalizedPage,
            normalizedPageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)normalizedPageSize));
    }
}
