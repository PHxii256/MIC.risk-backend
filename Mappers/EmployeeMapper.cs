using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MIC.risk.DTOs;
using MIC.risk.Models;


namespace MIC.risk.Mappers
{
    public static class EmployeeMapper
    {
        public static EmployeeResponseDto ToDto(this Employee employee)
        {
            return new EmployeeResponseDto(
                employee.Id,
                employee.IdentityUserId,
                employee.IdentityUser?.Email ?? string.Empty,
                employee.Name,
                employee.Department != null ? employee.Department.ToDto() : null!,
                employee.Active,
                employee.CreatedAt
            );
        }

        public static Employee ToEntity(this CreateEmployeeRequestDto dto, string identityUserId)
        {
            return new Employee
            {
                IdentityUserId = identityUserId,
                Name = dto.Name,
                DeptId = dto.DeptId,
                Active = true
            };
        }
    }
}