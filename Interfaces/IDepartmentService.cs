using MIC.risk.DTOs;

namespace MIC.risk.Services.Interfaces;

public interface IDepartmentService
{
    Task<IEnumerable<DepartmentResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<DepartmentResponseDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<DepartmentResponseDto> CreateAsync(CreateDepartmentRequestDto dto, CancellationToken cancellationToken = default);
    Task<DepartmentResponseDto?> UpdateAsync(long id, CreateDepartmentRequestDto dto, CancellationToken cancellationToken = default);
}
