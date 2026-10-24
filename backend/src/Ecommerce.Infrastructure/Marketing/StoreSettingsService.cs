using Ecommerce.Application.Marketing;
using Ecommerce.Application.Marketing.Dtos;
using Ecommerce.Domain.Marketing;
using Ecommerce.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Marketing;

public class StoreSettingsService(
    AppDbContext dbContext,
    IValidator<UpdateStoreSettingsRequest> updateValidator) : IStoreSettingsService
{
    public async Task<StoreSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        var settings = await GetOrCreateAsync(cancellationToken);
        return ToDto(settings);
    }

    public async Task<StoreSettingsDto> UpdateAsync(UpdateStoreSettingsRequest request, CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var settings = await GetOrCreateAsync(cancellationToken);
        settings.InstagramUrl = NullIfBlank(request.InstagramUrl);
        settings.TikTokUrl = NullIfBlank(request.TikTokUrl);
        settings.FacebookUrl = NullIfBlank(request.FacebookUrl);
        settings.ContactPhone = NullIfBlank(request.ContactPhone);
        settings.AnnouncementTextFr = NullIfBlank(request.AnnouncementTextFr);
        settings.AnnouncementTextAr = NullIfBlank(request.AnnouncementTextAr);
        settings.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(settings);
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private async Task<StoreSettings> GetOrCreateAsync(CancellationToken cancellationToken)
    {
        var settings = await dbContext.StoreSettings.FirstOrDefaultAsync(cancellationToken);
        if (settings is not null) return settings;

        settings = new StoreSettings();
        dbContext.StoreSettings.Add(settings);
        await dbContext.SaveChangesAsync(cancellationToken);
        return settings;
    }

    private static StoreSettingsDto ToDto(StoreSettings settings) => new(
        settings.InstagramUrl,
        settings.TikTokUrl,
        settings.FacebookUrl,
        settings.ContactPhone,
        settings.AnnouncementTextFr,
        settings.AnnouncementTextAr);
}
