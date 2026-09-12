using Ecommerce.Domain.Orders;

namespace Ecommerce.Application.Orders.Dtos;

/// <summary>
/// Staff-created order (e.g. a phone call). Deliberately a separate type from the guest-facing
/// <see cref="CreateOrderRequest"/> — <see cref="ManualDiscountAmount"/>/<see cref="FreeShipping"/> let
/// an admin apply a phone-negotiated price reduction, and that capability must never be reachable
/// through the public, [AllowAnonymous] order-creation endpoint (CLAUDE.md section 41: never trust the
/// frontend for prices/discounts — a field like this on the guest DTO would be a direct price-tampering
/// vector).
/// </summary>
public record CreateAdminOrderRequest(
    string FirstName,
    string LastName,
    string Phone,
    string Wilaya,
    string Commune,
    string? Address,
    DeliveryType DeliveryType,
    string? Notes,
    IReadOnlyList<OrderItemRequest> Items,
    string? CouponCode,
    /// <summary>Fixed DA amount negotiated off the subtotal (e.g. "I'll do it for 500 DA less"),
    /// on top of whatever automatic promotions still apply. Capped at the remaining discountable
    /// subtotal server-side — never trusted to leave the total negative.</summary>
    decimal? ManualDiscountAmount,
    /// <summary>Negotiated free shipping, independent of any FreeShipping promotion.</summary>
    bool FreeShipping);
