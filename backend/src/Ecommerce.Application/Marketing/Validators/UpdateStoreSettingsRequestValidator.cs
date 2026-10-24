using Ecommerce.Application.Marketing.Dtos;
using FluentValidation;

namespace Ecommerce.Application.Marketing.Validators;

public class UpdateStoreSettingsRequestValidator : AbstractValidator<UpdateStoreSettingsRequest>
{
    public UpdateStoreSettingsRequestValidator()
    {
        RuleFor(x => x.InstagramUrl).MaximumLength(500);
        RuleFor(x => x.TikTokUrl).MaximumLength(500);
        RuleFor(x => x.FacebookUrl).MaximumLength(500);
        RuleFor(x => x.ContactPhone).MaximumLength(20);
        RuleFor(x => x.AnnouncementTextFr).MaximumLength(200);
        RuleFor(x => x.AnnouncementTextAr).MaximumLength(200);
    }
}
