using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MIC.risk.DTOs;
using MIC.risk.Models;

namespace MIC.risk.Mappers
{
    public static class MiscMappers
    {

        public static DepartmentResponseDto ToDto(this Department department)
        {
            return new DepartmentResponseDto(
                department.Id,
                department.Name,
                department.BranchLocation
            );
        }

        public static Department ToEntity(this CreateDepartmentRequestDto dto)
        {
            return new Department
            {
                Name = dto.Name,
                BranchLocation = dto.BranchLocation
            };
        }
    }
}