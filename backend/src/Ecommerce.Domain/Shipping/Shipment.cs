using Ecommerce.Domain.Common;
using Ecommerce.Domain.Orders;

namespace Ecommerce.Domain.Shipping;

public class Shipment : Entity
{
    public Guid OrderId { get; set; }
    public ShippingCarrier Carrier { get; set; }
    public string ProviderShipmentId { get; set; } = string.Empty;
    public string? TrackingNumber { get; set; }
    public string ProviderStatus { get; set; } = string.Empty;
    public NormalizedShippingStatus NormalizedStatus { get; set; } = NormalizedShippingStatus.Created;

    public Order Order { get; set; } = null!;
    public ICollection<ShipmentTrackingEvent> TrackingEvents { get; set; } = [];
}
