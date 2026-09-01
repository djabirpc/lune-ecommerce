using Ecommerce.Domain.Orders;

namespace Ecommerce.Application.Orders.Dtos;

public record CreateOrderRequest(
    string FirstName,
    string LastName,
    string Phone,
    string Wilaya,
    string Commune,
    string Address,
    DeliveryType DeliveryType,
    string? Notes,
    IReadOnlyList<OrderItemRequest> Items);
