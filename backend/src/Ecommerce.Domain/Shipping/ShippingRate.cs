using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Shipping;

/// <summary>
/// The merchant's own per-wilaya delivery pricing — not fetched from any carrier (no real Yalidine/ZR
/// Express rate API is available, see IShippingProvider), but a real, admin-editable business field
/// used to compute Order.ShippingCost instead of hardcoding it to 0.
/// </summary>
public class ShippingRate : Entity
{
    public string Wilaya { get; set; } = string.Empty;
    public decimal HomeDeliveryPrice { get; set; }
    public decimal StopDeskPrice { get; set; }
    public bool IsActive { get; set; } = true;
}
