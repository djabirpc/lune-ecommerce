using Ecommerce.Application.Marketing.Dtos;
using FluentValidation;

namespace Ecommerce.Application.Marketing.Validators;

public class UpdateMarketingSettingsRequestValidator : AbstractValidator<UpdateMarketingSettingsRequest>
{
    public UpdateMarketingSettingsRequestValidator()
    {
        RuleFor(x => x.PurchaseEventTrigger).IsInEnum();
        RuleFor(x => x.MetaPixelId).MaximumLength(50);
        RuleFor(x => x.MetaAccessToken).MaximumLength(1000);
        RuleFor(x => x.TikTokPixelCode).MaximumLength(50);
        RuleFor(x => x.TikTokAccessToken).MaximumLength(1000);
    }
}
