using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MIC.risk.DTOs
{
    public record DepartmentResponseDto(
        long Id,
        string Name,
        string BranchLocation
    );

    public record CreateDepartmentRequestDto(
        string Name,
        string BranchLocation
    );
}