using Ecommerce.Application.Marketing.Dtos;

namespace Ecommerce.Application.Marketing;

/// <summary>Real, publicly documented Meta Graph API contract (Conversions API) — not a guessed
/// endpoint. Needs a Pixel ID + a Conversions API access token (distinct from the client-side Pixel
/// ID alone) to actually send anything; see MetaConversionsApiClient for the request shape.</summary>
public interface IMetaConversionsApiClient
{
    Task SendPurchaseEventAsync(string pixelId, string accessToken, PurchaseEventData data, CancellationToken cancellationToken = default);
}

/// <summary>Publicly documented TikTok Business "Events API" contract, reconstructed from public
/// TikTok for Business documentation rather than fetched live — field names/version should be
/// double-checked against the account's actual TikTok Business docs before relying on this in
/// production (same caution as CLAUDE.md section 17 asks for ZR Express).</summary>
public interface ITikTokEventsApiClient
{
    Task SendPurchaseEventAsync(string pixelCode, string accessToken, PurchaseEventData data, CancellationToken cancellationToken = default);
}
