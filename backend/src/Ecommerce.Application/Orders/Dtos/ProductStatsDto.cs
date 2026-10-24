namespace Ecommerce.Application.Orders.Dtos;

/// <summary>Units ordered / amount for one color or one size value of a product — "ordered", not
/// "sold", since this counts every order regardless of status (matches ProductOrderHistoryItemDto's
/// own scope) rather than only Delivered ones; CLAUDE.md section 42 — never invent a "confirmed
/// sales" number this data doesn't actually support without a stricter status filter.</summary>
public record ProductVariantStatDto(string Value, int UnitsOrdered, decimal Amount);

/// <summary>Per-product statistics for the admin catalog's product detail view — real aggregates
/// over every OrderItem matched by the product's Slug (same matching as ProductOrderHistoryItemDto),
/// broken down by color and separately by size.</summary>
public record ProductStatsDto(
    int TotalOrders,
    int TotalUnitsOrdered,
    decimal TotalAmount,
    IReadOnlyList<ProductVariantStatDto> ByColor,
    IReadOnlyList<ProductVariantStatDto> BySize);
