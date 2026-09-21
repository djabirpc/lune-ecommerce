using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Shipping;

/// <summary>
/// A merchant-entered reference note of what a given carrier actually charges for a wilaya —
/// informational only, never fetched automatically (no real carrier exposes a rates/tariffs API, see
/// CLAUDE.md section 16/PROJECT_CONTEXT.md's "never invent endpoints" rule) and never used to compute
/// Order.ShippingCost (that stays driven solely by ShippingRate, which the customer is actually
/// charged at checkout, before any carrier is chosen).
/// </summary>
public class CarrierShippingRate : Entity
{
    public ShippingCarrier Carrier { get; set; }
    public string Wilaya { get; set; } = string.Empty;
    public decimal? HomeDeliveryPrice { get; set; }
    public decimal? StopDeskPrice { get; set; }
    public string? Notes { get; set; }
}
