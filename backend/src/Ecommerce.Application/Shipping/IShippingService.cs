using Ecommerce.Application.Shipping.Dtos;

namespace Ecommerce.Application.Shipping;

public interface IShippingService
{
    Task<ShipmentDto> CreateShipmentAsync(Guid orderId, CreateShipmentRequest request, CancellationToken cancellationToken = default);

    Task<ShipmentDto> SyncTrackingAsync(Guid shipmentId, CancellationToken cancellationToken = default);

    Task<string> GetLabelAsync(Guid shipmentId, CancellationToken cancellationToken = default);

    IReadOnlyList<ShippingCarrierAvailabilityDto> GetCarrierAvailability();
}
