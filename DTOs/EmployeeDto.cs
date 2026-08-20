using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MIC.risk.DTOs
{
    public record CreateEmployeeRequestDto(
    string Email,
    string Name,
    long DeptId,
    string Role
    );

    public record UpdateEmployeeRequestDto(
        string Name,
        long DeptId,
        bool Active
    );

    public record EmployeeResponseDto(
        long Id,
        string IdentityUserId,
        string Email,
        string Name,
        DepartmentResponseDto Department,
        bool Active,
        DateTimeOffset CreatedAt
    );
}
