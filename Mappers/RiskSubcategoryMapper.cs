using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MIC.risk.DTOs;
using MIC.risk.Models;

namespace MIC.risk.Mappers
{
    public static class RiskSubcategoryMapper
    {
        public static RiskSubcategoryResponseDto ToDto(this RiskSubCategory riskSubcategory)
        {
            return new RiskSubcategoryResponseDto(
                riskSubcategory.Id,
                riskSubcategory.Name,
                riskSubcategory.Category
            );
        }

        public static RiskSubCategory ToEntity(this CreateRiskSubcategoryRequestDto dto)
        {
            return new RiskSubCategory
            {
                Name = dto.Name,
                Category = dto.Category,
                Active = true
            };
        }

    }
}