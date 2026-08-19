using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MIC.risk.DTOs
{
    public record CreateResourceRequestDto(
        string Name,
        long UploadedByEmpId,
        string Url,
        string Type,
        string? Description = null
    );

    public record PatchResourceRequestDto(
        string? Name = null,
        string? Description = null
    );

    public record ResourceResponseDto(
        long Id,
        string Name,
        EmployeeResponseDto UploadedBy,
        string Url,
        string Type,
        string? Description,
        DateTimeOffset UploadedAt
    );
}