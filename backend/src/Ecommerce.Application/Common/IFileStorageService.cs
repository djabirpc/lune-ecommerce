namespace Ecommerce.Application.Common;

/// <summary>
/// Abstraction over file storage (CLAUDE.md section 28) — never store large images directly in
/// Postgres. LocalFileStorageService is the dev/prototype implementation; swap in an S3/Cloudinary/
/// Azure Blob implementation later without touching any calling code.
/// </summary>
public interface IFileStorageService
{
    /// <summary>Saves the file and returns its public, directly-browsable URL.</summary>
    Task<string> SaveAsync(UploadFileRequest file, CancellationToken cancellationToken = default);

    Task DeleteAsync(string url, CancellationToken cancellationToken = default);
}

public record UploadFileRequest(Stream Content, string FileName, string ContentType, long LengthBytes);
