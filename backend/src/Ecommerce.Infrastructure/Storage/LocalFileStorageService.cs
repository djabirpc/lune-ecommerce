using Ecommerce.Application.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;

namespace Ecommerce.Infrastructure.Storage;

/// <summary>
/// Dev/prototype IFileStorageService implementation — writes to local disk and serves the result via
/// the static-file mapping configured in Program.cs. Mandatory swap point: a real deployment should
/// configure S3/Cloudinary/Azure Blob instead, per CLAUDE.md section 28.
/// </summary>
public class LocalFileStorageService(IWebHostEnvironment environment, IOptions<FileStorageOptions> options) : IFileStorageService
{
    private readonly FileStorageOptions _options = options.Value;

    public async Task<string> SaveAsync(UploadFileRequest file, CancellationToken cancellationToken = default)
    {
        var directory = _options.ResolveDirectory(environment.ContentRootPath);
        Directory.CreateDirectory(directory);

        var extension = Path.GetExtension(file.FileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(directory, storedFileName);

        await using (var fileStream = File.Create(fullPath))
        {
            await file.Content.CopyToAsync(fileStream, cancellationToken);
        }

        return $"{_options.PublicBaseUrl.TrimEnd('/')}/uploads/{storedFileName}";
    }

    public Task DeleteAsync(string url, CancellationToken cancellationToken = default)
    {
        var fileName = url[(url.LastIndexOf('/') + 1)..];
        var fullPath = Path.Combine(_options.ResolveDirectory(environment.ContentRootPath), fileName);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}
