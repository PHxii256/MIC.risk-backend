namespace MIC.risk.Options;

public class FileUploadOptions
{
    public const string SectionName = "FileUpload";

    public long MaxFileSizeBytes { get; set; } = 40 * 1024 * 1024;

    /// <summary>The URL path uploaded files are served from.</summary>
    public string UploadSubdirectory { get; set; } = "uploads";

    /// <summary>
    /// Where uploaded files are written. Left empty it resolves to a sibling of the project
    /// directory rather than somewhere beneath it.
    ///
    /// This must stay outside the project tree. The Web SDK globs the project directory, so a
    /// file appearing under it at runtime changes the project item set, and dotnet watch crashes
    /// re-evaluating that ("Unexpected true - HotReloadMSBuildWorkspace.cs"). Keeping user
    /// content out of the source tree is the right shape regardless: it survives a clean, and it
    /// is never swept into publish output.
    /// </summary>
    public string RootPath { get; set; } = string.Empty;

    public Dictionary<string, string[]> AllowedExtensionsByType { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Image"] = [".png", ".jpg", ".jpeg", ".gif", ".webp"],
        ["File"] = [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".csv", ".mp4", ".mp3", ".av1", ".m4a"]
    };
}
