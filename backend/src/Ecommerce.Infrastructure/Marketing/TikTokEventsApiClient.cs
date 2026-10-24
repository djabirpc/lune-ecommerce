using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ecommerce.Application.Marketing;
using Ecommerce.Application.Marketing.Dtos;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Marketing;

/// <summary>
/// TikTok for Business "Events API" (server-side pixel events), endpoint and field names taken from
/// TikTok's public Events API v1.3 documentation. Unlike the 48h Express/Ecotrack adapter, this was
/// NOT verified against a real merchant collection or a live test event — double-check field names
/// and the API version against the account's actual TikTok Business docs before relying on this
/// against real ad spend (CLAUDE.md section 17's caution, applied here too).
/// </summary>
public class TikTokEventsApiClient(HttpClient httpClient, ILogger<TikTokEventsApiClient> logger) : ITikTokEventsApiClient
{
    private const string EventsApiUrl = "https://business-api.tiktok.com/open_api/v1.3/pixel/track/";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task SendPurchaseEventAsync(string pixelCode, string accessToken, PurchaseEventData data, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            pixel_code = pixelCode,
            @event = "CompletePayment",
            event_id = data.OrderId.ToString(),
            timestamp = data.EventTimeUtc.ToString("o"),
            context = new { user = new { phone_number = PhoneHashing.HashedPhone(data.Phone) } },
            properties = new { currency = "DZD", value = data.Total },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, EventsApiUrl)
        {
            Content = JsonContent.Create(payload, options: JsonOptions),
        };
        request.Headers.Add("Access-Token", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning(
                "TikTok Events API rejected the Purchase event for order {OrderNumber}: {Status} {Body}",
                data.OrderNumber, response.StatusCode, body);
            throw new HttpRequestException($"TikTok Events API returned {response.StatusCode}.");
        }
    }
}
