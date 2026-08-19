using Microsoft.EntityFrameworkCore;
using MIC.risk.Data;
using MIC.risk.DTOs;
using MIC.risk.Mappers;
using MIC.risk.Models;
using MIC.risk.Services.Interfaces;

namespace MIC.risk.Services;

public class ResourceEngagementService : IResourceEngagementService
{
    private readonly ApplicationDBContext _context;

    public ResourceEngagementService(ApplicationDBContext context)
    {
        _context = context;
    }

    public async Task<ResourceEngagementResponseDto> UpsertAsync(
        RecordResourceEngagementRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        var employeeExists = await _context.Employees.AnyAsync(e => e.Id == dto.EmpId && e.Active, cancellationToken);
        if (!employeeExists)
        {
            throw new InvalidOperationException($"Active employee with ID {dto.EmpId} does not exist.");
        }

        var resource = await _context.Resources.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == dto.ResourceId && r.Active, cancellationToken);
        if (resource == null)
        {
            throw new InvalidOperationException($"Active resource with ID {dto.ResourceId} does not exist.");
        }

        if (resource.Type.Equals("Quiz", StringComparison.OrdinalIgnoreCase))
        {
            if (dto.SurveyCompleted == null)
            {
                throw new InvalidOperationException("SurveyCompleted is required for Quiz resources.");
            }
        }
        else if (dto.SurveyCompleted != null)
        {
            throw new InvalidOperationException("SurveyCompleted is only applicable to Quiz resources.");
        }

        var now = DateTimeOffset.UtcNow;
        var engagement = await _context.ResourceEngagements
            .FirstOrDefaultAsync(re => re.EmpId == dto.EmpId && re.ResourceId == dto.ResourceId, cancellationToken);

        if (engagement == null)
        {
            engagement = new ResourceEngagement
            {
                EmpId = dto.EmpId,
                ResourceId = dto.ResourceId,
                Viewed = dto.Viewed,
                SurveyCompleted = dto.SurveyCompleted,
                ViewedAt = dto.Viewed ? now : null,
                CompletedAt = dto.SurveyCompleted == true ? now : null
            };
            _context.ResourceEngagements.Add(engagement);
        }
        else
        {
            engagement.Viewed = dto.Viewed;
            engagement.SurveyCompleted = dto.SurveyCompleted;
            if (dto.Viewed)
            {
                engagement.ViewedAt ??= now;
            }

            if (dto.SurveyCompleted == true)
            {
                engagement.CompletedAt ??= now;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return (await GetEngagementDtoAsync(engagement.Id, cancellationToken))!;
    }

    public async Task<IEnumerable<ResourceEngagementResponseDto>> GetByEmployeeIdAsync(
        long empId,
        CancellationToken cancellationToken = default)
    {
        var engagements = await _context.ResourceEngagements
            .AsNoTracking()
            .Include(re => re.Employee).ThenInclude(e => e.Department)
            .Include(re => re.Employee).ThenInclude(e => e.IdentityUser)
            .Include(re => re.Resource).ThenInclude(r => r.Employee).ThenInclude(e => e.Department)
            .Include(re => re.Resource).ThenInclude(r => r.Employee).ThenInclude(e => e.IdentityUser)
            .Where(re => re.EmpId == empId)
            .ToListAsync(cancellationToken);

        return engagements.Select(re => re.ToDto());
    }

    public async Task<IEnumerable<ResourceEngagementStatsDto>> GetResourceStatsAsync(
        CancellationToken cancellationToken = default)
    {
        // Administrators curate the library rather than consume it, so they count towards
        // neither the readership nor the target.
        var adminUserIds = await (
            from userRole in _context.UserRoles
            join role in _context.Roles on userRole.RoleId equals role.Id
            where role.NormalizedName == "ADMIN"
            select userRole.UserId).ToListAsync(cancellationToken);

        var eligibleEmployeeIds = await _context.Employees
            .AsNoTracking()
            .Where(e => e.Active && !adminUserIds.Contains(e.IdentityUserId))
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        var eligibleEmployeeCount = eligibleEmployeeIds.Count;
        var eligibleSet = eligibleEmployeeIds.ToHashSet();

        var resources = await _context.Resources.AsNoTracking().Where(r => r.Active).ToListAsync(cancellationToken);
        var engagements = await _context.ResourceEngagements.AsNoTracking().ToListAsync(cancellationToken);

        return resources.Select(resource =>
        {
            var resourceEngagements = engagements
                .Where(e => e.ResourceId == resource.Id && eligibleSet.Contains(e.EmpId))
                .ToList();
            var viewCount = resourceEngagements.Count(e => e.Viewed);
            var quizCompletionCount = resource.Type.Equals("Quiz", StringComparison.OrdinalIgnoreCase)
                ? resourceEngagements.Count(e => e.SurveyCompleted == true)
                : 0;

            var completionRate = eligibleEmployeeCount == 0
                ? 0
                : resource.Type.Equals("Quiz", StringComparison.OrdinalIgnoreCase)
                    ? (double)quizCompletionCount / eligibleEmployeeCount * 100
                    : (double)viewCount / eligibleEmployeeCount * 100;

            return new ResourceEngagementStatsDto(
                resource.Id,
                resource.Name,
                resource.Type,
                viewCount,
                quizCompletionCount,
                Math.Round(completionRate, 2),
                eligibleEmployeeCount);
        });
    }

    public async Task<IEnumerable<DepartmentEngagementStatsDto>> GetEngagementByDepartmentAsync(
        CancellationToken cancellationToken = default)
    {
        var departments = await _context.Departments.AsNoTracking().ToListAsync(cancellationToken);
        var employees = await _context.Employees.AsNoTracking().Where(e => e.Active).ToListAsync(cancellationToken);

        var quizResourceIds = await _context.Resources.AsNoTracking()
            .Where(r => r.Type == "Quiz")
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        var completedEngagements = await _context.ResourceEngagements.AsNoTracking()
            .Where(re => quizResourceIds.Contains(re.ResourceId) && re.SurveyCompleted == true)
            .Select(re => re.EmpId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return departments.Select(department =>
        {
            var deptEmployees = employees.Where(e => e.DeptId == department.Id).ToList();
            var activeCount = deptEmployees.Count;
            var completedCount = deptEmployees.Count(e => completedEngagements.Contains(e.Id));
            var awareness = activeCount == 0 ? 0 : (double)completedCount / activeCount * 100;

            return new DepartmentEngagementStatsDto(
                department.Id,
                department.Name,
                activeCount,
                completedCount,
                Math.Round(awareness, 2));
        });
    }

    private async Task<ResourceEngagementResponseDto?> GetEngagementDtoAsync(long id, CancellationToken cancellationToken)
    {
        var engagement = await _context.ResourceEngagements
            .AsNoTracking()
            .Include(re => re.Employee).ThenInclude(e => e.Department)
            .Include(re => re.Employee).ThenInclude(e => e.IdentityUser)
            .Include(re => re.Resource).ThenInclude(r => r.Employee).ThenInclude(e => e.Department)
            .Include(re => re.Resource).ThenInclude(r => r.Employee).ThenInclude(e => e.IdentityUser)
            .FirstOrDefaultAsync(re => re.Id == id, cancellationToken);

        return engagement?.ToDto();
    }
}
