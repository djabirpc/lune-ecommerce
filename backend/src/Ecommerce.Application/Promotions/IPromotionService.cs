using Ecommerce.Application.Common;
using Ecommerce.Application.Promotions.Dtos;

namespace Ecommerce.Application.Promotions;

public interface IPromotionService
{
    Task<PromotionDetailDto> CreateAsync(SavePromotionRequest request, CancellationToken cancellationToken = default);

    Task<PromotionDetailDto> UpdateAsync(Guid id, SavePromotionRequest request, CancellationToken cancellationToken = default);

    Task<PromotionDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<PromotionDto>> GetPagedAsync(
        bool includeInactive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Currently active, in-window, non-coupon promotions — for public storefront display.</summary>
    Task<IReadOnlyList<PromotionDto>> GetActiveAsync(CancellationToken cancellationToken = default);
}
