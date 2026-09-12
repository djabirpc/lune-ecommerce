using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Marketing;
using Ecommerce.Application.Marketing.Dtos;
using Ecommerce.Domain.Marketing;
using Ecommerce.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Marketing;

public class HomeBannerService(
    AppDbContext dbContext,
    IFileStorageService fileStorageService,
    IValidator<UpdateHomeBannerRequest> updateValidator) : IHomeBannerService
{
    private static readonly string[] AllowedImageContentTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxImageSizeBytes = 5 * 1024 * 1024;

    public async Task<IReadOnlyList<HomeBannerDto>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        await dbContext.HomeBanners.AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.DisplayOrder)
            .Select(b => ToDto(b))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<HomeBannerDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.HomeBanners.AsNoTracking()
            .OrderBy(b => b.DisplayOrder)
            .Select(b => ToDto(b))
            .ToListAsync(cancellationToken);

    public async Task<HomeBannerDto> AddAsync(UploadFileRequest file, string? linkUrl, CancellationToken cancellationToken = default)
    {
        if (file.LengthBytes <= 0)
        {
            throw new ValidationAppException("Aucun fichier n'a été fourni.");
        }

        if (!AllowedImageContentTypes.Contains(file.ContentType))
        {
            throw new ValidationAppException("Format d'image non supporté. Utilisez JPEG, PNG ou WebP.");
        }

        if (file.LengthBytes > MaxImageSizeBytes)
        {
            throw new ValidationAppException("L'image ne doit pas dépasser 5 Mo.");
        }

        var url = await fileStorageService.SaveAsync(file, cancellationToken);

        var nextDisplayOrder = await dbContext.HomeBanners.AnyAsync(cancellationToken)
            ? await dbContext.HomeBanners.MaxAsync(b => b.DisplayOrder, cancellationToken) + 1
            : 0;

        var banner = new HomeBanner
        {
            ImageUrl = url,
            LinkUrl = string.IsNullOrWhiteSpace(linkUrl) ? null : linkUrl,
            DisplayOrder = nextDisplayOrder,
            IsActive = true,
        };

        dbContext.HomeBanners.Add(banner);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(banner);
    }

    public async Task<HomeBannerDto> UpdateAsync(Guid id, UpdateHomeBannerRequest request, CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var banner = await dbContext.HomeBanners.FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new NotFoundAppException("Bannière introuvable.");

        banner.LinkUrl = string.IsNullOrWhiteSpace(request.LinkUrl) ? null : request.LinkUrl;
        banner.DisplayOrder = request.DisplayOrder;
        banner.IsActive = request.IsActive;
        banner.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(banner);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var banner = await dbContext.HomeBanners.FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new NotFoundAppException("Bannière introuvable.");

        dbContext.HomeBanners.Remove(banner);
        await dbContext.SaveChangesAsync(cancellationToken);

        await fileStorageService.DeleteAsync(banner.ImageUrl, cancellationToken);
    }

    private static HomeBannerDto ToDto(HomeBanner b) => new(b.Id, b.ImageUrl, b.LinkUrl, b.DisplayOrder, b.IsActive);
}
