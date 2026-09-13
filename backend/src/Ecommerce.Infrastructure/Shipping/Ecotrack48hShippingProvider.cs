using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Shipping;
using Ecommerce.Domain.Shipping;
using Microsoft.Extensions.Options;

namespace Ecommerce.Infrastructure.Shipping;

/// <summary>
/// 48h Express, built on the white-label "Ecotrack" API — real documentation exists (the merchant
/// shared a Postman collection), unlike Yalidine/ZRExpress. Endpoints/fields below all come directly
/// from that collection; anything not explicitly confirmed by a real request/response example is
/// called out in a comment rather than guessed.
///
/// Two documented gaps worth knowing about before trusting this against production traffic:
/// 1. Auth is inconsistent across the collection's own examples — most endpoints show the token as
///    an `api_token` query parameter (confirmed by the "validate token" and "orders/status"
///    endpoints), but two config-listing endpoints show a Bearer header instead. This adapter uses
///    the query-parameter form since it's the dominant pattern and matches the endpoints actually
///    used here (create/validate/track).
/// 2. `POST /create/order`'s *success* response shape isn't shown anywhere in the collection (only
///    a validation-error example is) — this adapter infers it from the sibling bulk endpoint
///    (`POST /create/orders`), whose per-item success shape is `{"success": true, "tracking": "..."}`.
///    Verify this against a real account once credentials are available (Ecotrack48hOptions).
///
/// GetTrackingAsync deliberately calls `GET /get/orders/status` rather than the more obviously-named
/// `GET /get/tracking/info` — the latter's success shape also isn't documented (only its error
/// example is shown), while `/get/orders/status` has a full, real sample response to parse against.
/// </summary>
public class Ecotrack48hShippingProvider(HttpClient httpClient, IOptions<Ecotrack48hOptions> options) : IShippingProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    // Ecotrack's numeric wilaya_id matches AlgerianWilayas.All's index exactly (1-based) — confirmed
    // by GET /get/wilayas's sample response (1=Adrar, 2=Chlef, 3=Laghouat... in the same order).
    private static int ResolveWilayaCode(string wilaya)
    {
        var index = AlgerianWilayas.All.ToList().IndexOf(wilaya);
        return index < 0
            ? throw new ValidationAppException($"La wilaya \"{wilaya}\" n'est pas reconnue par l'intégration 48h Express.")
            : index + 1;
    }

    // Maps Ecotrack's French/snake_case order status (GET /get/orders/status) to our normalized
    // enum. Several distinct Ecotrack statuses collapse onto the same normalized value here (e.g.
    // livre_non_encaisse/encaisse_non_paye/paiements_prets/paye_et_archive are all post-delivery
    // payment/reconciliation states — our system only tracks the shipment up through Delivered).
    private static readonly Dictionary<string, NormalizedShippingStatus> StatusMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["prete_a_expedier"] = NormalizedShippingStatus.Created,
        ["en_preparation_stock"] = NormalizedShippingStatus.Created,
        ["en_ramassage"] = NormalizedShippingStatus.PickedUp,
        ["vers_hub"] = NormalizedShippingStatus.InTransit,
        ["en_hub"] = NormalizedShippingStatus.InTransit,
        ["vers_wilaya"] = NormalizedShippingStatus.InTransit,
        ["en_preparation"] = NormalizedShippingStatus.AtDestination,
        ["en_livraison"] = NormalizedShippingStatus.OutForDelivery,
        ["suspendu"] = NormalizedShippingStatus.Failed,
        ["livre_non_encaisse"] = NormalizedShippingStatus.Delivered,
        ["encaisse_non_paye"] = NormalizedShippingStatus.Delivered,
        ["paiements_prets"] = NormalizedShippingStatus.Delivered,
        ["paye_et_archive"] = NormalizedShippingStatus.Delivered,
        ["retour_chez_livreur"] = NormalizedShippingStatus.Returned,
        ["retour_transit_entrepot"] = NormalizedShippingStatus.Returned,
        ["retour_en_traitement"] = NormalizedShippingStatus.Returned,
        ["retour_recu"] = NormalizedShippingStatus.Returned,
        ["retour_archive"] = NormalizedShippingStatus.Returned,
        ["annule"] = NormalizedShippingStatus.Cancelled,
    };

    public ShippingCarrier Carrier => ShippingCarrier.Ecotrack48h;

    public bool IsConfigured => options.Value.IsConfigured;

    public async Task<ShippingProviderResult> CreateShipmentAsync(ShipmentRequest request, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var wilayaCode = ResolveWilayaCode(request.Wilaya);
        var fullName = $"{request.RecipientFirstName} {request.RecipientLastName}".Trim();

        var createQuery = BuildQuery(new Dictionary<string, string?>
        {
            ["api_token"] = options.Value.ApiToken,
            ["reference"] = request.OrderNumber,
            ["nom_client"] = fullName,
            ["telephone"] = request.Phone,
            ["adresse"] = string.IsNullOrWhiteSpace(request.Address) ? request.Commune : request.Address,
            ["commune"] = request.Commune,
            ["code_wilaya"] = wilayaCode.ToString(CultureInfo.InvariantCulture),
            ["montant"] = request.CodAmount.ToString("F0", CultureInfo.InvariantCulture),
            // type: 1 = Livraison — the only Ecotrack order type this app creates. Pickup/Échange/
            // Recouvrement exist in their API but have no equivalent concept in our order model.
            ["type"] = "1",
        });

        var createResponse = await SendAsync(HttpMethod.Post, $"/api/v1/create/order?{createQuery}", cancellationToken);
        var created = await ParseJsonAsync<CreateOrderResponse>(createResponse, cancellationToken);

        if (created.Success != true || string.IsNullOrWhiteSpace(created.Tracking))
        {
            var reason = created.Errors is { Count: > 0 }
                ? string.Join(" ", created.Errors.SelectMany(kv => kv.Value))
                : created.Message ?? "Réponse inattendue de 48h Express.";
            throw new ExternalServiceAppException($"48h Express a refusé la création de la commande : {reason}");
        }

        var tracking = created.Tracking;

        // Ecotrack's create/order only drafts the shipment (still editable/deletable) — valid/order
        // is what actually dispatches it to the carrier. Our system considers the order "Shipped"
        // the moment CreateShipmentAsync returns, so both calls happen together here to match that.
        var validateQuery = BuildQuery(new Dictionary<string, string?>
        {
            ["api_token"] = options.Value.ApiToken,
            ["tracking"] = tracking,
        });
        var validateResponse = await SendAsync(HttpMethod.Post, $"/api/v1/valid/order?{validateQuery}", cancellationToken);
        var validated = await ParseJsonAsync<SimpleSuccessResponse>(validateResponse, cancellationToken);

        if (validated.Success != true)
        {
            throw new ExternalServiceAppException(
                $"La commande 48h Express {tracking} a été créée mais n'a pas pu être validée/expédiée : {validated.Message ?? "réponse inattendue."}");
        }

        return new ShippingProviderResult(tracking, tracking, "prete_a_expedier", NormalizedShippingStatus.Created);
    }

    public async Task<ShippingTrackingResult> GetTrackingAsync(string providerShipmentId, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var query = BuildQuery(new Dictionary<string, string?>
        {
            ["api_token"] = options.Value.ApiToken,
            ["trackings"] = providerShipmentId,
        });

        var response = await SendAsync(HttpMethod.Get, $"/api/v1/get/orders/status?{query}", cancellationToken);
        var result = await ParseJsonAsync<OrdersStatusResponse>(response, cancellationToken);

        if (result.Data is null || !result.Data.TryGetValue(providerShipmentId, out var order))
        {
            throw new ExternalServiceAppException($"48h Express n'a retourné aucune information pour le colis {providerShipmentId}.");
        }

        var normalized = order.Status is not null && StatusMap.TryGetValue(order.Status, out var mapped)
            ? mapped
            : NormalizedShippingStatus.Unknown;

        var latestActivity = order.Activity?.LastOrDefault();
        var description = latestActivity is not null
            ? string.Join(" — ", new[] { latestActivity.Reason, latestActivity.Details, latestActivity.Station }.Where(s => !string.IsNullOrWhiteSpace(s)))
            : null;

        return new ShippingTrackingResult(order.Status ?? "unknown", normalized, string.IsNullOrWhiteSpace(description) ? null : description);
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
        {
            throw new NotConfiguredAppException(
                "48h Express n'est pas configuré (Ecotrack48h__BaseUrl / Ecotrack48h__ApiToken manquants).");
        }
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string relativeUrl, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(method, relativeUrl);
            return await httpClient.SendAsync(request, cancellationToken);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new ExternalServiceAppException("Impossible de contacter 48h Express (délai dépassé ou réseau indisponible).");
        }
    }

    private static async Task<T> ParseJsonAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken) where T : class, new()
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            throw new ExternalServiceAppException("48h Express : trop de requêtes envoyées (limite de 50/minute dépassée).");
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new ExternalServiceAppException("48h Express a rejeté le token d'authentification.");
        }

        try
        {
            return JsonSerializer.Deserialize<T>(body, JsonOptions) ?? new T();
        }
        catch (JsonException)
        {
            // Unrecognized/unexpected shape (CLAUDE.md section 43) — surface the real status code and
            // a truncated body rather than crashing on a malformed-JSON exception with no context.
            throw new ExternalServiceAppException(
                $"48h Express a renvoyé une réponse inattendue (HTTP {(int)response.StatusCode}) : {body[..Math.Min(body.Length, 300)]}");
        }
    }

    private static string BuildQuery(Dictionary<string, string?> parameters) =>
        string.Join("&", parameters
            .Where(kv => kv.Value is not null)
            .Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value!)}"));

    private class CreateOrderResponse
    {
        public bool? Success { get; set; }
        public string? Tracking { get; set; }
        public string? Message { get; set; }
        public Dictionary<string, string[]>? Errors { get; set; }
    }

    private class SimpleSuccessResponse
    {
        public bool? Success { get; set; }
        public string? Message { get; set; }
    }

    private class OrdersStatusResponse
    {
        public Dictionary<string, OrderStatusEntry>? Data { get; set; }
    }

    private class OrderStatusEntry
    {
        public string? Status { get; set; }

        [JsonPropertyName("order_id")]
        public string? OrderId { get; set; }

        public List<OrderActivityEntry>? Activity { get; set; }
    }

    private class OrderActivityEntry
    {
        public string? Reason { get; set; }
        public string? Details { get; set; }
        public string? Station { get; set; }
        public string? Driver { get; set; }
        public string? Date { get; set; }
        public string? Time { get; set; }
    }
}
