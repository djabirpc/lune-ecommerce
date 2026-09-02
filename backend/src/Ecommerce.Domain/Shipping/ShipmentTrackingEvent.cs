using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Shipping;

public class ShipmentTrackingEvent : Entity
{
    public Guid ShipmentId { get; set; }
    public string ProviderStatus { get; set; } = string.Empty;
    public NormalizedShippingStatus NormalizedStatus { get; set; }
    public string? Description { get; set; }
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

    public Shipment Shipment { get; set; } = null!;
}
