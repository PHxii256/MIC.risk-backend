using MIC.risk.DTOs;

namespace MIC.risk.Services.Interfaces;

public interface IRiskActionService
{
    Task<PagedResultDto<RiskActionResponseDto>> GetAllAsync(int page, int pageSize, CancellationToken cancellationToken = default);
    Task<RiskActionResponseDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<IEnumerable<RiskActionResponseDto>> GetByReportIdAsync(long reportId, CancellationToken cancellationToken = default);
    Task<RiskActionSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);
    Task<RiskActionResponseDto> CreateAsync(CreateRiskActionRequestDto dto, CancellationToken cancellationToken = default);
    Task<RiskActionResponseDto?> UpdateAsync(long id, UpdateRiskActionRequestDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
}
