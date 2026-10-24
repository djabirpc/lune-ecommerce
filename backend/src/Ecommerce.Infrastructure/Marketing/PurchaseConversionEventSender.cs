using Ecommerce.Application.Marketing;
using Ecommerce.Application.Marketing.Dtos;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ecommerce.Infrastructure.Marketing;

/// <summary>
/// Reads raw credentials directly from the DB (not through IMarketingSettingsService's masked DTO —
/// that's deliberately secret-free) and fans the event out to whichever of Meta/TikTok are configured.
/// Every failure is caught and logged here already — OrderService still wraps its call in a try/catch
/// as defense-in-depth, but this method itself never throws for a single provider's failure.
/// </summary>
public class PurchaseConversionEventSender(
    AppDbContext dbContext,
    IMetaConversionsApiClient metaClient,
    ITikTokEventsApiClient tikTokClient,
    ILogger<PurchaseConversionEventSender> logger) : IPurchaseConversionEventSender
{
    public async Task SendAsync(PurchaseEventData data, CancellationToken cancellationToken = default)
    {
        var settings = await dbContext.MarketingSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (settings is null) return;

        var tasks = new List<Task>();

        if (!string.IsNullOrWhiteSpace(settings.MetaPixelId) && !string.IsNullOrWhiteSpace(settings.MetaAccessToken))
        {
            var pixelId = settings.MetaPixelId;
            var accessToken = settings.MetaAccessToken;
            tasks.Add(SendSafelyAsync(
                () => metaClient.SendPurchaseEventAsync(pixelId, accessToken, data, cancellationToken),
                "Meta", data.OrderNumber));
        }

        if (!string.IsNullOrWhiteSpace(settings.TikTokPixelCode) && !string.IsNullOrWhiteSpace(settings.TikTokAccessToken))
        {
            var pixelCode = settings.TikTokPixelCode;
            var accessToken = settings.TikTokAccessToken;
            tasks.Add(SendSafelyAsync(
                () => tikTokClient.SendPurchaseEventAsync(pixelCode, accessToken, data, cancellationToken),
                "TikTok", data.OrderNumber));
        }

        if (tasks.Count == 0)
        {
            logger.LogInformation(
                "Purchase conversion event for order {OrderNumber} not sent: neither Meta nor TikTok is configured.",
                data.OrderNumber);
            return;
        }

        await Task.WhenAll(tasks);
    }

    private async Task SendSafelyAsync(Func<Task> send, string provider, string orderNumber)
    {
        try
        {
            await send();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send {Provider} Purchase conversion event for order {OrderNumber}.", provider, orderNumber);
        }
    }
}
