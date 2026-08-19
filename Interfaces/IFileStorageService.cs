using Microsoft.AspNetCore.Http;

namespace MIC.risk.Interfaces;

public record StoredFileResult(
    string RelativeUrl,
    string StoredFileName,
    long SizeBytes,
    string ResourceType);

public record StoredFileReadResult(
    Stream Content,
    string ContentType,
    string StoredFileName);

public interface IFileStorageService
{
    Task<StoredFileResult> SaveAsync(IFormFile file, CancellationToken cancellationToken = default);
    Task<StoredFileReadResult?> OpenReadAsync(
        string relativeUrl,
        CancellationToken cancellationToken = default);
}
