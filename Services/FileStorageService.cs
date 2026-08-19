using MIC.risk.Interfaces;
using MIC.risk.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.StaticFiles;

namespace MIC.risk.Services;

public class FileStorageService : IFileStorageService
{
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

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

    public Task<StoredFileReadResult?> OpenReadAsync(
        string relativeUrl,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var requestPath = relativeUrl.Split(['?', '#'], 2)[0].Replace('\\', '/');
        var uploadPrefix = $"/{_options.UploadSubdirectory.Trim('/')}/";

        // Resource URLs can also point to external sites. Only URLs produced by SaveAsync are
        // allowed to resolve into the upload directory, and nested/traversal paths are rejected.
        if (!requestPath.StartsWith(uploadPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<StoredFileReadResult?>(null);
        }

        var storedFileName = requestPath[uploadPrefix.Length..];
        if (string.IsNullOrWhiteSpace(storedFileName) ||
            !string.Equals(Path.GetFileName(storedFileName), storedFileName, StringComparison.Ordinal))
        {
            return Task.FromResult<StoredFileReadResult?>(null);
        }

        var uploadsRoot = UploadPath.Resolve(_options, _environment.ContentRootPath);
        var fullPath = Path.GetFullPath(Path.Combine(uploadsRoot, storedFileName));
        var rootPrefix = Path.GetFullPath(uploadsRoot).TrimEnd(Path.DirectorySeparatorChar) +
            Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
        {
            return Task.FromResult<StoredFileReadResult?>(null);
        }

        try
        {
            Stream content = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 64 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            if (!ContentTypeProvider.TryGetContentType(storedFileName, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            return Task.FromResult<StoredFileReadResult?>(
                new StoredFileReadResult(content, contentType, storedFileName));
        }
        catch (FileNotFoundException)
        {
            return Task.FromResult<StoredFileReadResult?>(null);
        }
        catch (DirectoryNotFoundException)
        {
            return Task.FromResult<StoredFileReadResult?>(null);
        }
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
