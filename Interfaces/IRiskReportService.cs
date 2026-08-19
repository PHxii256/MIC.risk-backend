using MIC.risk.DTOs;
using MIC.risk.Models;

namespace MIC.risk.Services.Interfaces;

public interface IRiskReportService
{
    Task<RiskReport?> GetEntityByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<RiskReportResponseDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<PagedResultDto<RiskReportResponseDto>> GetAllAsync(string? status, int page, int pageSize, string? search = null, string? sortBy = null, string? sortDir = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<RiskReportResponseDto>> GetByEmployeeIdAsync(long empId, CancellationToken cancellationToken = default);
    Task<RiskReportResponseDto> CreateReportAsync(CreateRiskReportRequestDto dto, CancellationToken cancellationToken = default);
    Task<RiskReportResponseDto?> AttachAuditorEvaluationAsync(long reportId, CreateEvaluationRequestDto dto, long auditorEmpId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revises the auditor evaluation already attached to a report, re-stamping it with the
    /// auditor who made the change and when, so the report can show who last revised it.
    /// </summary>
    Task<RiskReportResponseDto?> UpdateAuditorEvaluationAsync(long reportId, CreateEvaluationRequestDto dto, long auditorEmpId, CancellationToken cancellationToken = default);
    Task<RiskReportResponseDto?> UpdateStatusAsync(long reportId, string newStatus, long changedByEmpId, CancellationToken cancellationToken = default);
    Task<PagedResultDto<RiskReportStatusHistoryResponseDto>> GetStatusHistoryAsync(long reportId, int page, int pageSize, CancellationToken cancellationToken = default);
}
