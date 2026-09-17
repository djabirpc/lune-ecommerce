using Ecommerce.Domain.Orders;

namespace Ecommerce.Application.Orders.Dtos;

public record OrderStatusHistoryDto(
    Guid Id,
    OrderStatus OldStatus,
    OrderStatus NewStatus,
    string? Reason,
    DateTime CreatedAtUtc,
    // Null for a transition with no acting user (e.g. shipping-status sync driving the order forward
    // automatically) or if the resolving lookup didn't find a match.
    string? ChangedByUserName);
