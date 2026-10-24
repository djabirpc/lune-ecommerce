using Ecommerce.Application.Marketing.Dtos;

namespace Ecommerce.Application.Marketing;

public interface IMarketingService
{
    /// <summary>Orders grouped by UtmSource (orders with no source are bucketed as "Direct"), over the last N days.</summary>
    Task<IReadOnlyList<MarketingSourceSummaryDto>> GetSourceSummaryAsync(int days, CancellationToken cancellationToken = default);
}

public interface IMarketingSettingsService
{
    Task<MarketingSettingsDto> GetAsync(CancellationToken cancellationToken = default);

    Task<MarketingSettingsDto> UpdateAsync(UpdateMarketingSettingsRequest request, CancellationToken cancellationToken = default);

    /// <summary>Lightweight, no-secrets read used by the public (unauthenticated) storefront endpoint
    /// that CheckoutPage calls to decide whether to fire the "Purchase" pixel event itself.</summary>
    Task<Domain.Marketing.PurchaseEventTrigger> GetPurchaseEventTriggerAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Fans a confirmed order's "Purchase" conversion event out to whichever of Meta/TikTok have real
/// credentials configured. Deliberately swallow-and-log on every failure path (see implementation) —
/// a marketing pixel being down must never block an order confirmation.
/// </summary>
public interface IPurchaseConversionEventSender
{
    Task SendAsync(Dtos.PurchaseEventData data, CancellationToken cancellationToken = default);
}

/// <summary>General storefront content (social links, contact phone, announcement bar) — nothing
/// secret, so GetAsync() is also called from the public, unauthenticated endpoint the storefront uses.</summary>
public interface IStoreSettingsService
{
    Task<Dtos.StoreSettingsDto> GetAsync(CancellationToken cancellationToken = default);

    Task<Dtos.StoreSettingsDto> UpdateAsync(Dtos.UpdateStoreSettingsRequest request, CancellationToken cancellationToken = default);
}
