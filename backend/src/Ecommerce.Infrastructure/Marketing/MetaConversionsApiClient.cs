using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ecommerce.Application.Marketing;
using Ecommerce.Application.Marketing.Dtos;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Marketing;

/// <summary>
/// Real, documented Meta Graph API endpoint: POST /{pixel-id}/events, access_token as a query
/// parameter (Meta's own contract — not a shortcut taken here). action_source is "phone_call" since
/// that's literally how a COD order gets confirmed (an agent calling the customer), which is also the
/// most accurate of Meta's fixed action_source values for this flow.
/// </summary>
public class MetaConversionsApiClient(HttpClient httpClient, ILogger<MetaConversionsApiClient> logger) : IMetaConversionsApiClient
{
    private const string ApiVersion = "v21.0";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public async Task SendPurchaseEventAsync(string pixelId, string accessToken, PurchaseEventData data, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            data = new[]
            {
                new
                {
                    event_name = "Purchase",
                    event_time = new DateTimeOffset(data.EventTimeUtc, TimeSpan.Zero).ToUnixTimeSeconds(),
                    action_source = "phone_call",
                    event_id = data.OrderId.ToString(),
                    user_data = new { ph = new[] { PhoneHashing.HashedPhone(data.Phone) } },
                    custom_data = new { currency = "DZD", value = data.Total, order_id = data.OrderNumber },
                },
            },
        };

        using var response = await httpClient.PostAsJsonAsync(
            $"https://graph.facebook.com/{ApiVersion}/{pixelId}/events?access_token={Uri.EscapeDataString(accessToken)}",
            payload,
            JsonOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning(
                "Meta Conversions API rejected the Purchase event for order {OrderNumber}: {Status} {Body}",
                data.OrderNumber, response.StatusCode, body);
            throw new HttpRequestException($"Meta Conversions API returned {response.StatusCode}.");
        }
    }
}
