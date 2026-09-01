using Ecommerce.Domain.Orders;

namespace Ecommerce.Application.Orders.Dtos;

public record OrderStatusHistoryDto(
    Guid Id,
    OrderStatus OldStatus,
    OrderStatus NewStatus,
    string? Reason,
    DateTime CreatedAtUtc);
