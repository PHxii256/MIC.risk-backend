using Microsoft.AspNetCore.Http;

namespace MIC.risk.Interfaces;

public record StoredFileResult(
    string RelativeUrl,
    string StoredFileName,
    long SizeBytes,
    string ResourceType);

public interface IFileStorageService
{
    Task<StoredFileResult> SaveAsync(IFormFile file, CancellationToken cancellationToken = default);
}
