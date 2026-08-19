using MIC.risk.Options;

namespace MIC.risk.Services;

/// <summary>
/// Resolves the single directory uploaded files live in, so the writer and the static-file
/// pipeline cannot disagree about where that is.
/// </summary>
public static class UploadPath
{
    /// <summary>
    /// An absolute path outside the project tree.
    ///
    /// Configure <c>FileUpload:RootPath</c> for a real deployment. The default sits beside the
    /// project rather than inside it, because anything written under the project directory at
    /// runtime changes the MSBuild item set and crashes <c>dotnet watch</c>.
    /// </summary>
    public static string Resolve(FileUploadOptions options, string contentRootPath)
    {
        var configured = options.RootPath;

        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Path.GetFullPath(configured);
        }

        var projectName = new DirectoryInfo(contentRootPath).Name;

        return Path.GetFullPath(
            Path.Combine(contentRootPath, "..", $"{projectName}.uploads"));
    }
}
