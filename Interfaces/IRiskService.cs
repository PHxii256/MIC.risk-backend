using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MIC.risk.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace MIC.risk.Interfaces
{
    public interface IRiskService
    {
        Task<RiskSubcategoryResponseDto?> GetByIdAsync(long id);
        Task<IEnumerable<RiskSubcategoryResponseDto>> GetRiskSubcategoriesAsync(String category);
        Task<IEnumerable<RiskCategoryResponseDto>> GetAllRisksAsync();
        Task<RiskSubcategoryResponseDto> CreateRiskSubcategoryAsync(CreateRiskSubcategoryRequestDto dto);
        Task<RiskSubcategoryResponseDto?> UpdateRiskSubcategoryAsync(long id, UpdateRiskSubcategoryRequestDto dto);
        Task<bool> SoftDeleteAsync(long id);
    }
}