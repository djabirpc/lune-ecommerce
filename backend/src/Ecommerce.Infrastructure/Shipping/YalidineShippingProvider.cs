using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Shipping;
using Ecommerce.Domain.Shipping;
using Microsoft.Extensions.Options;

namespace Ecommerce.Infrastructure.Shipping;

/// <summary>
/// CLAUDE.md section 16: adapter structure only. Credentials are read from configuration
/// (YALIDINE_BASE_URL / YALIDINE_API_ID / YALIDINE_API_TOKEN), but the actual HTTP calls are
/// deliberately NOT implemented — Yalidine's real endpoint paths, request/response shapes, and
/// authentication header format are not documented anywhere in this codebase, and CLAUDE.md
/// explicitly forbids inventing them. Every method throws NotConfiguredAppException with a message
/// naming exactly what's missing. Once official API docs are available, implement the HTTP calls
/// here without changing the IShippingProvider contract or any caller.
/// </summary>
public class YalidineShippingProvider(IOptions<YalidineOptions> options) : IShippingProvider
{
    public ShippingCarrier Carrier => ShippingCarrier.Yalidine;

    /// <summary>
    /// Always false: credentials alone don't make this provider usable, since the actual HTTP
    /// integration isn't implemented (see class remarks). Check <see cref="YalidineOptions.IsConfigured"/>
    /// separately if you need to know whether credentials have at least been supplied.
    /// </summary>
    public bool IsConfigured => false;

    public Task<ShippingProviderResult> CreateShipmentAsync(ShipmentRequest request, CancellationToken cancellationToken = default) =>
        throw NotImplemented();

    public Task<ShippingTrackingResult> GetTrackingAsync(string providerShipmentId, CancellationToken cancellationToken = default) =>
        throw NotImplemented();

    private NotConfiguredAppException NotImplemented() => new(
        options.Value.IsConfigured
            ? "L'intégration Yalidine n'est pas encore implémentée : la documentation officielle de l'API Yalidine (endpoints, format d'authentification) n'était pas disponible au moment du développement. Utilisez le transporteur Fake en attendant."
            : "Yalidine n'est pas configuré (YALIDINE_BASE_URL / YALIDINE_API_ID / YALIDINE_API_TOKEN manquants), et l'intégration n'est de toute façon pas encore implémentée faute de documentation API officielle.");
}
