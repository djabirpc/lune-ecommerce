using Ecommerce.Domain.Promotions;

namespace Ecommerce.Application.Promotions.Dtos;

public record PromotionDto(
    Guid Id,
    string Name,
    string? Description,
    PromotionType Type,
    decimal? PercentageValue,
    decimal? FixedAmountValue,
    int? BuyQuantity,
    int? GetQuantity,
    int? BundleQuantity,
    decimal? BundleTotalPrice,
    bool HasCouponCode,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    bool IsActive,
    int Priority,
    IReadOnlyList<Guid> ProductIds,
    IReadOnlyList<Guid> CategoryIds,
    int? MinQuantity,
    bool IncludesFreeShipping);
