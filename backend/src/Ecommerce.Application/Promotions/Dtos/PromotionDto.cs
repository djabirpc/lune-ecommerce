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
    bool HasCouponCode,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    bool IsActive,
    int Priority);
