using Ecommerce.Domain.Orders;

namespace Ecommerce.Application.Orders.Dtos;

public record OrderSummaryDto(
    Guid Id,
    string OrderNumber,
    OrderStatus Status,
    string CustomerFullName,
    string Phone,
    string Wilaya,
    decimal Total,
    DateTime CreatedAtUtc);
