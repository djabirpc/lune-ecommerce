using Ecommerce.Domain.Orders;

namespace Ecommerce.Application.Orders.Dtos;

public record ChangeOrderStatusRequest(OrderStatus NewStatus, string? Reason);
