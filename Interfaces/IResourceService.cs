using MIC.risk.DTOs;
using Microsoft.AspNetCore.Http;

namespace MIC.risk.Services.Interfaces;

public interface IResourceService
{
    Task<IEnumerable<ResourceResponseDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ResourceResponseDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<ResourceResponseDto> CreateAsync(CreateResourceRequestDto dto, CancellationToken cancellationToken = default);
    Task<ResourceResponseDto?> PatchAsync(long id, PatchResourceRequestDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
    Task<ResourceFileResult?> GetFileAsync(
    long id,
    CancellationToken cancellationToken);
    Task<ResourceResponseDto> UploadAsync(long uploadedByEmpId, string name, IFormFile file, string? description = null, CancellationToken cancellationToken = default);
}
