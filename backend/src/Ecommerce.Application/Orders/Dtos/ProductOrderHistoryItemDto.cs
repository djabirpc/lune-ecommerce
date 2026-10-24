using Ecommerce.Domain.Orders;

namespace Ecommerce.Application.Orders.Dtos;

/// <summary>One order that contains a given product — summed across however many of that product's
/// variants (colors/sizes) appear as separate lines within that same order. Matched by the OrderItem's
/// snapshotted ProductSlug (not a live FK join to ProductVariants/Products), so a product's order
/// history survives even if its variants are later deleted — see Important Decision #22.</summary>
public record ProductOrderHistoryItemDto(
    Guid OrderId,
    string OrderNumber,
    DateTime CreatedAtUtc,
    string CustomerFullName,
    OrderStatus Status,
    int Quantity,
    decimal LineTotal);
