namespace Ecommerce.Infrastructure.Storage;

public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>Directory to write uploaded files to — relative to the app's content root, or absolute (e.g. a Docker volume mount).</summary>
    public string LocalPath { get; set; } = "uploads";

    /// <summary>Base URL the uploads directory is served from (must match Program.cs's static-file mapping).</summary>
    public string PublicBaseUrl { get; set; } = "http://localhost:5000";

    public string ResolveDirectory(string contentRootPath) =>
        Path.IsPathRooted(LocalPath) ? LocalPath : Path.Combine(contentRootPath, LocalPath);
}
