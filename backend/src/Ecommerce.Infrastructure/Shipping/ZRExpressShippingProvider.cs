using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Shipping;
using Ecommerce.Domain.Shipping;
using Microsoft.Extensions.Options;

namespace Ecommerce.Infrastructure.Shipping;

/// <summary>
/// CLAUDE.md section 17: adapter structure only, same rationale as YalidineShippingProvider —
/// no real ZR Express API documentation was available, so no endpoints/auth are guessed. ZR Express
/// must NOT be assumed to share Yalidine's contract; ZRExpressOptions uses deliberately generic
/// placeholder field names pending the merchant's actual API contract.
/// </summary>
public class ZRExpressShippingProvider(IOptions<ZRExpressOptions> options) : IShippingProvider
{
    public ShippingCarrier Carrier => ShippingCarrier.ZRExpress;

    /// <summary>
    /// Always false: credentials alone don't make this provider usable, since the actual HTTP
    /// integration isn't implemented (see class remarks). Check <see cref="ZRExpressOptions.IsConfigured"/>
    /// separately if you need to know whether credentials have at least been supplied.
    /// </summary>
    public bool IsConfigured => false;

    public Task<ShippingProviderResult> CreateShipmentAsync(ShipmentRequest request, CancellationToken cancellationToken = default) =>
        throw NotImplemented();

    public Task<ShippingTrackingResult> GetTrackingAsync(string providerShipmentId, CancellationToken cancellationToken = default) =>
        throw NotImplemented();

    private NotConfiguredAppException NotImplemented() => new(
        options.Value.IsConfigured
            ? "L'intégration ZR Express n'est pas encore implémentée : la documentation officielle de l'API ZR Express n'était pas disponible au moment du développement. Utilisez le transporteur Fake en attendant."
            : "ZR Express n'est pas configuré (ZR_EXPRESS_BASE_URL / ZR_EXPRESS_API_KEY manquants), et l'intégration n'est de toute façon pas encore implémentée faute de documentation API officielle.");
}
