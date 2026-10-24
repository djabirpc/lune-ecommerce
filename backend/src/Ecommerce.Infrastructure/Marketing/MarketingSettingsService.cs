using Ecommerce.Application.Marketing;
using Ecommerce.Application.Marketing.Dtos;
using Ecommerce.Domain.Marketing;
using Ecommerce.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Marketing;

public class MarketingSettingsService(
    AppDbContext dbContext,
    IValidator<UpdateMarketingSettingsRequest> updateValidator) : IMarketingSettingsService
{
    public async Task<MarketingSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateAsync(cancellationToken);
        return ToDto(settings);
    }

    public async Task<MarketingSettingsDto> UpdateAsync(UpdateMarketingSettingsRequest request, CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var settings = await GetOrCreateAsync(cancellationToken);

        settings.PurchaseEventTrigger = request.PurchaseEventTrigger;
        settings.MetaPixelId = request.MetaPixelId;
        settings.TikTokPixelCode = request.TikTokPixelCode;

        if (request.ClearMetaAccessToken)
        {
            settings.MetaAccessToken = null;
        }
        else if (!string.IsNullOrWhiteSpace(request.MetaAccessToken))
        {
            settings.MetaAccessToken = request.MetaAccessToken;
        }

        if (request.ClearTikTokAccessToken)
        {
            settings.TikTokAccessToken = null;
        }
        else if (!string.IsNullOrWhiteSpace(request.TikTokAccessToken))
        {
            settings.TikTokAccessToken = request.TikTokAccessToken;
        }

        settings.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(settings);
    }

    public async Task<PurchaseEventTrigger> GetPurchaseEventTriggerAsync(CancellationToken cancellationToken = default)
    {
        var settings = await dbContext.MarketingSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        return settings?.PurchaseEventTrigger ?? PurchaseEventTrigger.OrderCreated;
    }

    /// <summary>Single-row table — lazily creates the (one and only) row the first time anything asks
    /// for settings, instead of requiring a migration-time seed.</summary>
    private async Task<MarketingSettings> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        var settings = await dbContext.MarketingSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is not null) return settings;

        settings = new MarketingSettings();
        dbContext.MarketingSettings.Add(settings);
        await dbContext.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private static MarketingSettingsDto ToDto(MarketingSettings settings) => new(
        settings.PurchaseEventTrigger,
        settings.MetaPixelId,
        !string.IsNullOrWhiteSpace(settings.MetaAccessToken),
        settings.TikTokPixelCode,
        !string.IsNullOrWhiteSpace(settings.TikTokAccessToken));
}
