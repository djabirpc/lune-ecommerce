using Ecommerce.Application.Common;
using Ecommerce.Application.Marketing.Dtos;

namespace Ecommerce.Application.Marketing;

public interface IHomeBannerService
{
    /// <summary>Active banners, ordered by DisplayOrder — public, for the storefront hero carousel.</summary>
    Task<IReadOnlyList<HomeBannerDto>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>All banners including inactive ones, ordered by DisplayOrder — admin management list.</summary>
    Task<IReadOnlyList<HomeBannerDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<HomeBannerDto> AddAsync(UploadFileRequest file, string? linkUrl, CancellationToken cancellationToken = default);

    Task<HomeBannerDto> UpdateAsync(Guid id, UpdateHomeBannerRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
