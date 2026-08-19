using MIC.risk.Interfaces;
using MIC.risk.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace MIC.risk.Services;

public class FileStorageService : IFileStorageService
{
    private readonly IWebHostEnvironment _environment;
    private readonly FileUploadOptions _options;

    public FileStorageService(IWebHostEnvironment environment, IOptions<FileUploadOptions> options)
    {
        _environment = environment;
        _options = options.Value;
    }

    public async Task<StoredFileResult> SaveAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            throw new InvalidOperationException("No file was provided.");
        }

        if (file.Length > _options.MaxFileSizeBytes)
        {
            throw new InvalidOperationException(
                $"File exceeds the maximum allowed size of {_options.MaxFileSizeBytes / (1024 * 1024)} MB.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new InvalidOperationException("The uploaded file must have a recognized extension.");
        }

        var resourceType = ResolveResourceType(extension)
            ?? throw new InvalidOperationException(
                $"File type '{extension}' is not supported. Allowed: {GetAllowedExtensionsDescription()}.");

        // Deliberately outside the project tree; see UploadPath.
        var uploadsRoot = UploadPath.Resolve(_options, _environment.ContentRootPath);

        Directory.CreateDirectory(uploadsRoot);

        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(uploadsRoot, storedFileName);

        await using (var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var relativeUrl = $"/{_options.UploadSubdirectory}/{storedFileName}";
        return new StoredFileResult(relativeUrl, storedFileName, file.Length, resourceType);
    }

    private string? ResolveResourceType(string extension)
    {
        foreach (var (resourceType, allowedExtensions) in _options.AllowedExtensionsByType)
        {
            if (allowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
            {
                return resourceType;
            }
        }

        return null;
    }

    private string GetAllowedExtensionsDescription()
    {
        return string.Join(
            ", ",
            _options.AllowedExtensionsByType
                .SelectMany(pair => pair.Value.Select(ext => $"{ext} ({pair.Key})"))
                .OrderBy(ext => ext));
    }
}
