using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Shipping;
using Ecommerce.Application.Shipping.Dtos;
using Ecommerce.Domain.Shipping;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Ecommerce.Infrastructure.Shipping;

/// <summary>
/// Implements IEcotrack48hPreviewService against Ecotrack's real, documented read-only endpoints
/// (GET /get/orders, GET /get/tracking/info). Deliberately separate from Ecotrack48hShippingProvider
/// (the real, wired-in IShippingProvider) — this class is never called by order creation/checkout,
/// only by the admin-only preview controller, and only ever issues GET requests.
///
/// Auth: uses an `Authorization: Bearer {token}` header (configured on the typed HttpClient in
/// DependencyInjection.cs), per the collection's own "Authorisation" folder description — empirically
/// confirmed working against a real account. Ecotrack48hShippingProvider currently uses an `api_token`
/// query parameter instead for the endpoints it calls (its own doc comment flags this as an
/// unconfirmed gap) — worth reconciling once the real integration is revisited.
///
/// GET /get/orders is Ecotrack-side paginated (40/page) with no wilaya/status filter of its own, but
/// the wilaya map + dashboard both need to slice the *whole* order history every way at once — so
/// every page is fetched once, cached in-memory for a few minutes (IMemoryCache), and every filter/
/// pagination/aggregation the preview UI needs is then done in C# against that cached snapshot,
/// rather than re-hitting Ecotrack (and its 50-requests/minute limit) on every click.
/// </summary>
public class Ecotrack48hPreviewService(HttpClient httpClient, IOptions<Ecotrack48hOptions> options, IMemoryCache cache) : IEcotrack48hPreviewService
{
    private const string CacheKey = "ecotrack48h-preview-all-orders";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(3);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Ecotrack48hPreviewOrdersPageDto> GetOrdersAsync(
        Ecotrack48hPreviewFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var all = await GetAllOrdersAsync(cancellationToken);
        var filtered = ApplyFilter(all, filter).ToList();

        var lastPage = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)pageSize));
        page = Math.Clamp(page, 1, lastPage);

        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new Ecotrack48hPreviewOrdersPageDto(items, page, lastPage, filtered.Count);
    }

    public async Task<Ecotrack48hPreviewDashboardDto> GetDashboardAsync(Ecotrack48hPreviewFilter filter, CancellationToken cancellationToken = default)
    {
        var all = await GetAllOrdersAsync(cancellationToken);
        var filtered = ApplyFilter(all, filter).ToList();

        var byWilaya = filtered
            .Where(o => o.WilayaId is > 0)
            .GroupBy(o => o.WilayaId!.Value)
            .Select(g => new Ecotrack48hPreviewWilayaStatDto(g.Key, ResolveWilayaName(g.Key), g.Count(), g.Sum(o => o.Amount ?? 0m)))
            .OrderByDescending(w => w.Count)
            .ToList();

        var byStatus = filtered
            .GroupBy(o => o.Status)
            .Select(g => new Ecotrack48hPreviewStatusStatDto(g.Key, g.Count()))
            .OrderByDescending(s => s.Count)
            .ToList();

        return new Ecotrack48hPreviewDashboardDto(
            filtered.Count,
            filtered.Sum(o => o.Amount ?? 0m),
            filtered.Count(o => string.Equals(o.GlobalStatus, "livre", StringComparison.OrdinalIgnoreCase)),
            filtered.Count(o => string.Equals(o.GlobalStatus, "en_process", StringComparison.OrdinalIgnoreCase)),
            filtered.Count(o => string.Equals(o.GlobalStatus, "retour", StringComparison.OrdinalIgnoreCase)),
            byWilaya,
            byStatus);
    }

    // Status/WilayaId are conventionally only ever passed for the orders-table query, not the
    // dashboard one — see Ecotrack48hPreviewFilter's doc comment for why (keeps the map/stat cards
    // showing the full picture while the table narrows further). Date/amount/search narrow both.
    private static IEnumerable<Ecotrack48hPreviewOrderDto> ApplyFilter(IEnumerable<Ecotrack48hPreviewOrderDto> orders, Ecotrack48hPreviewFilter filter) =>
        orders
            .Where(o => filter.Status is null || string.Equals(o.Status, filter.Status, StringComparison.OrdinalIgnoreCase))
            .Where(o => filter.WilayaId is null || o.WilayaId == filter.WilayaId)
            .Where(o => filter.StartDate is null || (o.CreatedAtUtc is not null && o.CreatedAtUtc >= filter.StartDate))
            .Where(o => filter.EndDate is null || (o.CreatedAtUtc is not null && o.CreatedAtUtc <= filter.EndDate))
            .Where(o => filter.MinAmount is null || (o.Amount ?? 0m) >= filter.MinAmount)
            .Where(o => filter.MaxAmount is null || (o.Amount ?? 0m) <= filter.MaxAmount)
            .Where(o => string.IsNullOrWhiteSpace(filter.Search)
                || o.Tracking.Contains(filter.Search, StringComparison.OrdinalIgnoreCase)
                || o.Client.Contains(filter.Search, StringComparison.OrdinalIgnoreCase)
                || o.Phone.Contains(filter.Search, StringComparison.OrdinalIgnoreCase)
                || (o.Reference?.Contains(filter.Search, StringComparison.OrdinalIgnoreCase) ?? false));

    public async Task<Ecotrack48hPreviewTrackingDto> GetTrackingAsync(string tracking, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var response = await SendAsync(HttpMethod.Get, $"/api/v1/get/tracking/info?tracking={Uri.EscapeDataString(tracking)}", cancellationToken);
        var raw = await ParseJsonAsync<RawTrackingInfo>(response, cancellationToken);

        var activity = (raw.Activity ?? [])
            .Select(a => new Ecotrack48hPreviewTrackingEventDto(a.Status, string.IsNullOrWhiteSpace(a.Station) ? null : a.Station, ParseDateTime(a.Date, a.Time)))
            .ToList();

        return new Ecotrack48hPreviewTrackingDto(
            tracking,
            raw.RecipientName,
            string.IsNullOrWhiteSpace(raw.CurrentStation) ? null : raw.CurrentStation,
            activity);
    }

    public async Task<byte[]> GetLabelPdfAsync(string tracking, CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var response = await SendAsync(HttpMethod.Get, $"/api/v1/get/order/label?tracking={Uri.EscapeDataString(tracking)}", cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            throw new ExternalServiceAppException("48h Express : trop de requêtes envoyées (limite de 50/minute dépassée).");
        }

        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new ExternalServiceAppException("48h Express a rejeté le token d'authentification.");
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new NotFoundAppException($"Aucune étiquette 48h Express trouvée pour le suivi {tracking}.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new ExternalServiceAppException($"48h Express n'a pas pu générer l'étiquette pour {tracking} (HTTP {(int)response.StatusCode}).");
        }

        var contentType = response.Content.Headers.ContentType?.MediaType;
        var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);

        // Ecotrack's docs promise a raw PDF here — if it ever answers with something else (e.g. a JSON
        // error body with a 200 status), surface that clearly instead of silently handing back garbage
        // bytes as if they were a valid PDF.
        if (contentType is not null && !contentType.Contains("pdf", StringComparison.OrdinalIgnoreCase))
        {
            throw new ExternalServiceAppException($"48h Express a renvoyé une réponse inattendue pour l'étiquette de {tracking} (type : {contentType}).");
        }

        return bytes;
    }

    private async Task<List<Ecotrack48hPreviewOrderDto>> GetAllOrdersAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKey, out List<Ecotrack48hPreviewOrderDto>? cached) && cached is not null)
        {
            return cached;
        }

        EnsureConfigured();

        var all = new List<RawOrder>();
        var page = 1;
        int lastPage;
        do
        {
            var response = await SendAsync(HttpMethod.Get, $"/api/v1/get/orders?page={page}", cancellationToken);
            var raw = await ParseJsonAsync<RawOrdersPage>(response, cancellationToken);
            all.AddRange(raw.Data ?? []);
            lastPage = Math.Max(raw.LastPage, 1);
            page++;
        }
        while (page <= lastPage);

        var mapped = all.Select(ToOrderDto).ToList();
        cache.Set(CacheKey, mapped, CacheDuration);
        return mapped;
    }

    private static Ecotrack48hPreviewOrderDto ToOrderDto(RawOrder o) => new(
        o.Tracking ?? "",
        o.Reference,
        o.Client ?? "",
        o.Phone ?? "",
        o.Adresse,
        o.WilayaId,
        o.WilayaId is > 0 ? ResolveWilayaName(o.WilayaId.Value) : null,
        ParseDecimal(o.Montant),
        ParseDecimal(o.TarifPrestation),
        o.Products,
        o.Status ?? "inconnu",
        o.GlobalStatus ?? "inconnu",
        o.StopDesk == 1,
        ParseDate(o.CreatedAt),
        ParseDate(o.LastUpdatedAt),
        string.IsNullOrWhiteSpace(o.DriverName) ? null : o.DriverName);

    // Ecotrack's numeric wilaya_id matches AlgerianWilayas.All's index exactly (1-based) — same
    // confirmed mapping Ecotrack48hShippingProvider already relies on.
    private static string ResolveWilayaName(int wilayaId) =>
        wilayaId >= 1 && wilayaId <= AlgerianWilayas.All.Count ? AlgerianWilayas.All[wilayaId - 1] : "Inconnue";

    private void EnsureConfigured()
    {
        if (!options.Value.IsConfigured)
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
            throw new ExternalServiceAppException(
                $"48h Express a renvoyé une réponse inattendue (HTTP {(int)response.StatusCode}) : {body[..Math.Min(body.Length, 300)]}");
        }
    }

    private static decimal? ParseDecimal(string? value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var result) ? result : null;

    // Ecotrack's "get/orders" timestamps ("2026-09-17 11:25:05") have no explicit timezone — treated
    // as Algeria local time (UTC+1, no DST) since the whole platform is Algeria-only.
    private static DateTime? ParseDate(string? value) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)
            ? DateTime.SpecifyKind(result.AddHours(-1), DateTimeKind.Utc)
            : null;

    private static DateTime? ParseDateTime(string? date, string? time)
    {
        if (string.IsNullOrWhiteSpace(date))
        {
            return null;
        }

        var combined = string.IsNullOrWhiteSpace(time) ? date : $"{date} {time}";
        return DateTime.TryParse(combined, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)
            ? DateTime.SpecifyKind(result.AddHours(-1), DateTimeKind.Utc)
            : null;
    }

    private class RawOrdersPage
    {
        [JsonPropertyName("current_page")]
        public int CurrentPage { get; set; }

        public List<RawOrder>? Data { get; set; }

        [JsonPropertyName("last_page")]
        public int LastPage { get; set; }

        public int Total { get; set; }
    }

    private class RawOrder
    {
        public string? Tracking { get; set; }
        public string? Reference { get; set; }
        public string? Client { get; set; }
        public string? Phone { get; set; }
        public string? Adresse { get; set; }

        [JsonPropertyName("wilaya_id")]
        public int? WilayaId { get; set; }

        public string? Montant { get; set; }

        [JsonPropertyName("tarif_prestation")]
        public string? TarifPrestation { get; set; }

        public string? Products { get; set; }
        public string? Status { get; set; }

        [JsonPropertyName("global_status")]
        public string? GlobalStatus { get; set; }

        [JsonPropertyName("stop_desk")]
        public int? StopDesk { get; set; }

        [JsonPropertyName("created_at")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("last_updated_at")]
        public string? LastUpdatedAt { get; set; }

        [JsonPropertyName("driver_name")]
        public string? DriverName { get; set; }
    }

    private class RawTrackingInfo
    {
        [JsonPropertyName("recipientName")]
        public string? RecipientName { get; set; }

        [JsonPropertyName("currentStation")]
        public string? CurrentStation { get; set; }

        public List<RawTrackingActivity>? Activity { get; set; }
    }

    private class RawTrackingActivity
    {
        public string? Status { get; set; }
        public string? Station { get; set; }
        public string? Date { get; set; }
        public string? Time { get; set; }
    }
}
