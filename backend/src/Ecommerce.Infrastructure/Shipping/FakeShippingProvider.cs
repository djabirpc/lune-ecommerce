using System.Collections.Concurrent;
using System.Security.Cryptography;
using Ecommerce.Application.Shipping;
using Ecommerce.Domain.Shipping;

namespace Ecommerce.Infrastructure.Shipping;

/// <summary>
/// The mandatory dev/test double from CLAUDE.md section 15. Fully functional, but entirely
/// simulated — no network calls. Registered as a singleton so its in-memory progress dictionary
/// survives across requests within the running process, letting repeated tracking syncs advance
/// a shipment through a fixed status sequence deterministically.
///
/// Presented under the fictional brand "Atlas Express" in the admin/customer-facing UI (tracking
/// numbers, labels, carrier picker) instead of literally saying "Fake" — this is purely cosmetic,
/// the underlying enum value (ShippingCarrier.Fake), behavior, and determinism are unchanged. It
/// is not a real courier and is never implied to be one: <see cref="GetLabelAsync"/> in
/// ShippingService still stamps every label as a development-only simulation.
/// </summary>
public class FakeShippingProvider : IShippingProvider
{
    /// <summary>The fictional carrier name shown wherever this provider's shipments are displayed.</summary>
    public const string DisplayName = "Atlas Express";

    private static readonly (string Provider, NormalizedShippingStatus Normalized)[] Sequence =
    [
        ("CREATED", NormalizedShippingStatus.Created),
        ("PICKED_UP", NormalizedShippingStatus.PickedUp),
        ("IN_TRANSIT", NormalizedShippingStatus.InTransit),
        ("AT_DESTINATION", NormalizedShippingStatus.AtDestination),
        ("OUT_FOR_DELIVERY", NormalizedShippingStatus.OutForDelivery),
        ("DELIVERED", NormalizedShippingStatus.Delivered),
    ];

    private readonly ConcurrentDictionary<string, int> _progress = new();

    public ShippingCarrier Carrier => ShippingCarrier.Fake;

    public bool IsConfigured => true;

    public Task<ShippingProviderResult> CreateShipmentAsync(ShipmentRequest request, CancellationToken cancellationToken = default)
    {
        var providerShipmentId = $"ATL-{Guid.NewGuid():N}"[..13].ToUpperInvariant();
        var trackingNumber = $"ATL{RandomNumberGenerator.GetInt32(10_000_000, 99_999_999)}";
        _progress[providerShipmentId] = 0;

        var (providerStatus, normalizedStatus) = Sequence[0];
        return Task.FromResult(new ShippingProviderResult(providerShipmentId, trackingNumber, providerStatus, normalizedStatus));
    }

    public Task<ShippingTrackingResult> GetTrackingAsync(string providerShipmentId, CancellationToken cancellationToken = default)
    {
        var step = _progress.AddOrUpdate(providerShipmentId, 0, (_, current) => Math.Min(current + 1, Sequence.Length - 1));
        var (providerStatus, normalizedStatus) = Sequence[step];

        return Task.FromResult(new ShippingTrackingResult(
            providerStatus,
            normalizedStatus,
            $"{DisplayName} (simulation) : étape {step + 1}/{Sequence.Length}"));
    }
}
