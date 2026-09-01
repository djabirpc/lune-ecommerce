using Ecommerce.Domain.Promotions;

namespace Ecommerce.Application.Promotions.Dtos;

public record PromotionDetailDto(
    Guid Id,
    string Name,
    string? Description,
    PromotionType Type,
    decimal? PercentageValue,
    decimal? FixedAmountValue,
    int? BuyQuantity,
    int? GetQuantity,
    string? CouponCode,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    bool IsActive,
    int Priority,
    IReadOnlyList<Guid> ProductIds,
    IReadOnlyList<Guid> CategoryIds);
