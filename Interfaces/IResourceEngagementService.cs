using MIC.risk.DTOs;

namespace MIC.risk.Services.Interfaces;

public interface IResourceEngagementService
{
    Task<ResourceEngagementResponseDto> UpsertAsync(RecordResourceEngagementRequestDto dto, CancellationToken cancellationToken = default);
    Task<IEnumerable<ResourceEngagementResponseDto>> GetByEmployeeIdAsync(long empId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ResourceEngagementStatsDto>> GetResourceStatsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<DepartmentEngagementStatsDto>> GetEngagementByDepartmentAsync(CancellationToken cancellationToken = default);
}
