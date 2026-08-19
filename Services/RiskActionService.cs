using Microsoft.EntityFrameworkCore;
using MIC.risk.Data;
using MIC.risk.DTOs;
using MIC.risk.Mappers;
using MIC.risk.Models;
using MIC.risk.Services.Interfaces;

namespace MIC.risk.Services;

public class RiskActionService : IRiskActionService
{
    private static readonly HashSet<string> ValidStatuses = new(StringComparer.Ordinal)
    {
        "Pending", "Completed"
    };

    private readonly ApplicationDBContext _context;

    public RiskActionService(ApplicationDBContext context)
    {
        _context = context;
    }

    public async Task<PagedResultDto<RiskActionResponseDto>> GetAllAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var (normalizedPage, normalizedPageSize) = PaginationHelper.Normalize(page, pageSize);

        var query = _context.RiskActions.AsNoTracking()
            .Include(a => a.Assignee).ThenInclude(e => e.Department)
            .Include(a => a.Assignee).ThenInclude(e => e.IdentityUser)
            .OrderBy(a => a.DueDate);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((normalizedPage - 1) * normalizedPageSize)
            .Take(normalizedPageSize)
            .ToListAsync(cancellationToken);

        return new PagedResultDto<RiskActionResponseDto>(
            items.Select(a => a.ToDto()),
            normalizedPage,
            normalizedPageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)normalizedPageSize));
    }

    public async Task<RiskActionResponseDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var action = await _context.RiskActions.AsNoTracking()
            .Include(a => a.Assignee).ThenInclude(e => e.Department)
            .Include(a => a.Assignee).ThenInclude(e => e.IdentityUser)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

        return action?.ToDto();
    }

    public async Task<IEnumerable<RiskActionResponseDto>> GetByReportIdAsync(
        long reportId,
        CancellationToken cancellationToken = default)
    {
        var actions = await _context.RiskActions.AsNoTracking()
            .Include(a => a.Assignee).ThenInclude(e => e.Department)
            .Include(a => a.Assignee).ThenInclude(e => e.IdentityUser)
            .Where(a => a.ReportId == reportId)
            .OrderBy(a => a.DueDate)
            .ToListAsync(cancellationToken);

        return actions.Select(a => a.ToDto());
    }

    public async Task<RiskActionSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var weekEnd = now.AddDays(7);

        var pendingActions = await _context.RiskActions.AsNoTracking()
            .Where(a => a.Status == "Pending")
            .ToListAsync(cancellationToken);

        var overdue = pendingActions.Count(a => a.DueDate < now);
        var dueThisWeek = pendingActions.Count(a => a.DueDate >= now && a.DueDate <= weekEnd);

        return new RiskActionSummaryDto(overdue, dueThisWeek);
    }

    public async Task<RiskActionResponseDto> CreateAsync(
        CreateRiskActionRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        ValidateAction(dto.Title, "Pending");

        var reportExists = await _context.RiskReports.AnyAsync(r => r.Id == dto.ReportId, cancellationToken);
        if (!reportExists)
        {
            throw new InvalidOperationException($"Risk report with ID {dto.ReportId} does not exist.");
        }

        var assigneeExists = await _context.Employees.AnyAsync(e => e.Id == dto.AssigneeEmpId && e.Active, cancellationToken);
        if (!assigneeExists)
        {
            throw new InvalidOperationException($"Active assignee with ID {dto.AssigneeEmpId} does not exist.");
        }

        var entity = new RiskAction
        {
            ReportId = dto.ReportId,
            Title = dto.Title,
            Description = dto.Description,
            AssigneeEmpId = dto.AssigneeEmpId,
            DueDate = dto.DueDate,
            Status = "Pending"
        };

        _context.RiskActions.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<RiskActionResponseDto?> UpdateAsync(
        long id,
        UpdateRiskActionRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        ValidateAction(dto.Title, dto.Status);

        var action = await _context.RiskActions.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (action == null)
        {
            return null;
        }

        var assigneeExists = await _context.Employees.AnyAsync(e => e.Id == dto.AssigneeEmpId && e.Active, cancellationToken);
        if (!assigneeExists)
        {
            throw new InvalidOperationException($"Active assignee with ID {dto.AssigneeEmpId} does not exist.");
        }

        action.Title = dto.Title;
        action.Description = dto.Description;
        action.AssigneeEmpId = dto.AssigneeEmpId;
        action.DueDate = dto.DueDate;
        action.Status = dto.Status;
        action.CompletedAt = dto.Status == "Completed" ? DateTimeOffset.UtcNow : null;

        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var action = await _context.RiskActions.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (action == null)
        {
            return false;
        }

        _context.RiskActions.Remove(action);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void ValidateAction(string title, string status)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new InvalidOperationException("Action title is required.");
        }

        if (!ValidStatuses.Contains(status))
        {
            throw new InvalidOperationException("Action status must be Pending or Completed.");
        }
    }
}
