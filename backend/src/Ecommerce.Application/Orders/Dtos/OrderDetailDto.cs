using Ecommerce.Application.Shipping.Dtos;
using Ecommerce.Domain.Orders;

namespace Ecommerce.Application.Orders.Dtos;

public record OrderDetailDto(
    Guid Id,
    string OrderNumber,
    OrderStatus Status,
    string FirstName,
    string LastName,
    string Phone,
    string Wilaya,
    string Commune,
    string Address,
    DeliveryType DeliveryType,
    string? Notes,
    string PaymentMethod,
    PaymentStatus PaymentStatus,
    decimal Subtotal,
    decimal ShippingCost,
    decimal DiscountTotal,
    decimal Total,
    DateTime CreatedAtUtc,
    IReadOnlyList<OrderItemDto> Items,
    IReadOnlyList<OrderStatusHistoryDto> StatusHistory,
    IReadOnlyList<OrderCallAttemptDto> CallAttempts,
    IReadOnlyList<OrderPromotionDto> AppliedPromotions,
    ShipmentDto? Shipment,
    MarketingAttributionDto? MarketingAttribution);
