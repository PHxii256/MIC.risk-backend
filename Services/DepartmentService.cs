using Microsoft.EntityFrameworkCore;
using MIC.risk.Data;
using MIC.risk.DTOs;
using MIC.risk.Mappers;
using MIC.risk.Services.Interfaces;

namespace MIC.risk.Services;

public class DepartmentService : IDepartmentService
{
    private readonly ApplicationDBContext _context;

    public DepartmentService(ApplicationDBContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<DepartmentResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var departments = await _context.Departments
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ThenBy(d => d.BranchLocation)
            .ToListAsync(cancellationToken);

        return departments.Select(d => d.ToDto());
    }

    public async Task<DepartmentResponseDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var department = await _context.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

        return department?.ToDto();
    }

    public async Task<DepartmentResponseDto> CreateAsync(CreateDepartmentRequestDto dto, CancellationToken cancellationToken = default)
    {
        ValidateDepartment(dto);

        if (await DepartmentExistsAsync(dto.Name, dto.BranchLocation, excludeId: null, cancellationToken))
        {
            throw new InvalidOperationException(
                $"A department named '{dto.Name}' already exists at branch location '{dto.BranchLocation}'.");
        }

        var entity = dto.ToEntity();
        _context.Departments.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.ToDto();
    }

    public async Task<DepartmentResponseDto?> UpdateAsync(long id, CreateDepartmentRequestDto dto, CancellationToken cancellationToken = default)
    {
        var department = await _context.Departments.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (department == null)
        {
            return null;
        }

        ValidateDepartment(dto);

        if (await DepartmentExistsAsync(dto.Name, dto.BranchLocation, excludeId: id, cancellationToken))
        {
            throw new InvalidOperationException(
                $"A department named '{dto.Name}' already exists at branch location '{dto.BranchLocation}'.");
        }

        department.Name = dto.Name;
        department.BranchLocation = dto.BranchLocation;
        await _context.SaveChangesAsync(cancellationToken);

        return department.ToDto();
    }

    private static void ValidateDepartment(CreateDepartmentRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
        {
            throw new InvalidOperationException("Department name is required.");
        }

        if (string.IsNullOrWhiteSpace(dto.BranchLocation))
        {
            throw new InvalidOperationException("Branch location is required.");
        }
    }

    private async Task<bool> DepartmentExistsAsync(
        string name,
        string branchLocation,
        long? excludeId,
        CancellationToken cancellationToken)
    {
        return await _context.Departments.AnyAsync(
            d => d.Name == name
                && d.BranchLocation == branchLocation
                && (excludeId == null || d.Id != excludeId),
            cancellationToken);
    }
}
