using Microsoft.EntityFrameworkCore;
using MIC.risk.Data;
using MIC.risk.DTOs;
using MIC.risk.Interfaces;
using MIC.risk.Mappers;
using MIC.risk.Services.Interfaces;
using Microsoft.AspNetCore.Http;

namespace MIC.risk.Services;

public class ResourceService : IResourceService
{
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Video", "Image", "File", "Quiz", "Link"
    };

    private readonly ApplicationDBContext _context;
    private readonly IFileStorageService _fileStorageService;

    public ResourceService(ApplicationDBContext context, IFileStorageService fileStorageService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
    }

    public async Task<IEnumerable<ResourceResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var resources = await _context.Resources
            .AsNoTracking()
            .Include(r => r.Employee).ThenInclude(e => e.Department)
            .Include(r => r.Employee).ThenInclude(e => e.IdentityUser)
            .Where(r => r.Active)
            .OrderByDescending(r => r.UploadedAt)
            .ToListAsync(cancellationToken);

        return resources.Select(r => r.ToDto());
    }

    public async Task<ResourceResponseDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var resource = await _context.Resources
            .AsNoTracking()
            .Include(r => r.Employee).ThenInclude(e => e.Department)
            .Include(r => r.Employee).ThenInclude(e => e.IdentityUser)
            .FirstOrDefaultAsync(r => r.Id == id && r.Active, cancellationToken);

        return resource?.ToDto();
    }

    public async Task<ResourceResponseDto> CreateAsync(CreateResourceRequestDto dto, CancellationToken cancellationToken = default)
    {
        ValidateResource(dto.Name, dto.Url, dto.Type);

        var employeeExists = await _context.Employees.AnyAsync(e => e.Id == dto.UploadedByEmpId && e.Active, cancellationToken);
        if (!employeeExists)
        {
            throw new InvalidOperationException($"Employee with ID {dto.UploadedByEmpId} does not exist.");
        }

        var entity = dto.ToEntity();
        _context.Resources.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return (await GetByIdAsync(entity.Id, cancellationToken))!;
    }

    public async Task<ResourceResponseDto?> PatchAsync(long id, PatchResourceRequestDto dto, CancellationToken cancellationToken = default)
    {
        var resource = await _context.Resources.FirstOrDefaultAsync(r => r.Id == id && r.Active, cancellationToken);
        if (resource == null)
        {
            return null;
        }

        var hasName = dto.Name is not null;
        var hasDescription = dto.Description is not null;

        if (!hasName && !hasDescription)
        {
            throw new InvalidOperationException("Provide at least one of name or description to update.");
        }

        if (hasName)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new InvalidOperationException("Resource name cannot be blank.");
            }

            resource.Name = dto.Name;
        }

        if (hasDescription)
        {
            resource.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        var resource = await _context.Resources.FirstOrDefaultAsync(r => r.Id == id && r.Active, cancellationToken);
        if (resource == null)
        {
            return false;
        }

        resource.Active = false;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ResourceResponseDto> UploadAsync(
        long uploadedByEmpId,
        string name,
        IFormFile file,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        var stored = await _fileStorageService.SaveAsync(file, cancellationToken);

        var dto = new CreateResourceRequestDto(
            name,
            uploadedByEmpId,
            stored.RelativeUrl,
            stored.ResourceType,
            description);

        return await CreateAsync(dto, cancellationToken);
    }

    private static void ValidateResource(string name, string url, string type)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Resource name is required.");
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidOperationException("Resource URL is required.");
        }

        if (string.IsNullOrWhiteSpace(type) || !AllowedTypes.Contains(type))
        {
            throw new InvalidOperationException("Resource type must be one of: Video, Image, File, Quiz, Link.");
        }
    }

    public async Task<ResourceFileResult?> GetFileAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var resource = await _context.Resources
            .AsNoTracking()
            .Where(r => r.Id == id && r.Active && (r.Type == "File" || r.Type == "Image"))
            .Select(r => new { r.Name, r.Url })
            .FirstOrDefaultAsync(cancellationToken);

        if (resource == null)
        {
            return null;
        }

        var storedFile = await _fileStorageService.OpenReadAsync(resource.Url, cancellationToken);
        if (storedFile == null)
        {
            return null;
        }

        var fileName = BuildDownloadFileName(resource.Name, storedFile.StoredFileName);
        return new ResourceFileResult(storedFile.Content, storedFile.ContentType, fileName);
    }

    private static string BuildDownloadFileName(string resourceName, string storedFileName)
    {
        var safeName = Path.GetFileName(resourceName.Trim());
        foreach (var invalidCharacter in Path.GetInvalidFileNameChars())
        {
            safeName = safeName.Replace(invalidCharacter, '_');
        }

        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "resource";
        }

        var extension = Path.GetExtension(storedFileName);
        return safeName.EndsWith(extension, StringComparison.OrdinalIgnoreCase)
            ? safeName
            : $"{safeName}{extension}";
    }

}
