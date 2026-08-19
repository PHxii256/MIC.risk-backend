using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MIC.risk.Data;
using MIC.risk.DTOs;
using MIC.risk.Interfaces;
using MIC.risk.Mappers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MIC.risk.Services
{
    public class RiskService : IRiskService
    {
        private static readonly HashSet<string> AllowedCategories = new(StringComparer.OrdinalIgnoreCase)
        {
            "Financial",
            "Operational",
            "Strategic",
            "Insurance"
        };

        private readonly ApplicationDBContext _context;
        public RiskService(ApplicationDBContext context)
        {
            _context = context;
        }
        public async Task<RiskSubcategoryResponseDto> CreateRiskSubcategoryAsync(CreateRiskSubcategoryRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new InvalidOperationException("Please Provide A Valid Subcategory Name.");
            }

            if (string.IsNullOrWhiteSpace(dto.Category) ||
                !AllowedCategories.Contains(dto.Category))
            {
                throw new InvalidOperationException("Please Provide A Valid Category Name.");
            }

            var subCategoryExists = await _context.RiskSubCategories
                .AnyAsync(r => r.Name == dto.Name);

            if (subCategoryExists)
            {
                throw new InvalidOperationException("Subcategory Already Exists.");
            }

            var entity = dto.ToEntity();

            _context.RiskSubCategories.Add(entity);
            await _context.SaveChangesAsync();

            return entity.ToDto();
        }

        public async Task<RiskSubcategoryResponseDto?> UpdateRiskSubcategoryAsync(long id, UpdateRiskSubcategoryRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new InvalidOperationException("Please Provide A Valid Subcategory Name.");
            }

            if (string.IsNullOrWhiteSpace(dto.Category) ||
                !AllowedCategories.Contains(dto.Category))
            {
                throw new InvalidOperationException("Please Provide A Valid Category Name.");
            }

            var entity = await _context.RiskSubCategories
                .FirstOrDefaultAsync(sc => sc.Id == id && sc.Active);

            if (entity == null)
            {
                return null;
            }

            var duplicateName = await _context.RiskSubCategories
                .AnyAsync(r => r.Name == dto.Name && r.Id != id);

            if (duplicateName)
            {
                throw new InvalidOperationException("Subcategory Already Exists.");
            }

            entity.Name = dto.Name;
            entity.Category = dto.Category;
            await _context.SaveChangesAsync();

            return entity.ToDto();
        }

        public async Task<bool> SoftDeleteAsync(long id)
        {
            var entity = await _context.RiskSubCategories
                .FirstOrDefaultAsync(sc => sc.Id == id && sc.Active);

            if (entity == null)
            {
                return false;
            }

            entity.Active = false;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<IEnumerable<RiskCategoryResponseDto>> GetAllRisksAsync()
        {
            return await _context.RiskSubCategories
                .AsNoTracking()
                .Where(sc => sc.Active)
                .GroupBy(sc => sc.Category)
                .Select(g => new RiskCategoryResponseDto(
                    g.Key,
                    g.Select(sc => new RiskSubcategoryDto(
                        sc.Id,
                        sc.Name
                    ))
                ))
                .ToListAsync();
        }

        public async Task<RiskSubcategoryResponseDto?> GetByIdAsync(long id)
        {
            var entity = await _context.RiskSubCategories
                .AsNoTracking()
                .FirstOrDefaultAsync(sc => sc.Id == id && sc.Active);

            return entity?.ToDto();
        }

        public async Task<IEnumerable<RiskSubcategoryResponseDto>> GetRiskSubcategoriesAsync(string category)
        {
            var query = _context.RiskSubCategories.AsNoTracking().AsQueryable();
            if (string.IsNullOrWhiteSpace(category))
            {
                throw new InvalidOperationException($"Please Proivde A Valid Category Name.");
            }

            return await query
            .Where(sc => sc.Category == category && sc.Active)
            .Select(sc => new RiskSubcategoryResponseDto(sc.Id, sc.Name, sc.Category)).ToListAsync();
        }
    }
}