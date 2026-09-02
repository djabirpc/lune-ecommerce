using Ecommerce.Domain.Shipping;

namespace Ecommerce.Application.Shipping;

/// <summary>
/// Abstraction over a shipping carrier (CLAUDE.md section 15). Implementations must never invent
/// undocumented endpoints or authentication for a real carrier — see YalidineShippingProvider /
/// ZRExpressShippingProvider for the pattern to follow when real API docs/credentials aren't available.
/// </summary>
public interface IShippingProvider
{
    ShippingCarrier Carrier { get; }

    /// <summary>True if this provider has everything it needs (credentials, endpoint docs) to actually call a real carrier.</summary>
    bool IsConfigured { get; }

    Task<ShippingProviderResult> CreateShipmentAsync(ShipmentRequest request, CancellationToken cancellationToken = default);

    Task<ShippingTrackingResult> GetTrackingAsync(string providerShipmentId, CancellationToken cancellationToken = default);
}
