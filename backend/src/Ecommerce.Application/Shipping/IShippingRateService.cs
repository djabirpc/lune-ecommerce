using Ecommerce.Application.Shipping.Dtos;
using Ecommerce.Domain.Orders;

namespace Ecommerce.Application.Shipping;

/// <summary>
/// The merchant's own per-wilaya delivery pricing (see ShippingRate) — deliberately separate from
/// IShippingProvider, which is about talking to a carrier's API, not what the merchant charges.
/// </summary>
public interface IShippingRateService
{
    Task<IReadOnlyList<ShippingRateDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ShippingRateDto> UpdateAsync(string wilaya, UpdateShippingRateRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the authoritative shipping price for a wilaya/delivery-type pair. Throws
    /// ValidationAppException if the wilaya is unknown or its rate has been deactivated — an order
    /// must never be created with a silently-wrong (e.g. zero) shipping cost (CLAUDE.md section 41).
    /// </summary>
    Task<decimal> GetPriceAsync(string wilaya, DeliveryType deliveryType, CancellationToken cancellationToken = default);
}
