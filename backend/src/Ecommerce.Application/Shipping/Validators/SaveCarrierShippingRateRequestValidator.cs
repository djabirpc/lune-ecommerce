using Ecommerce.Application.Shipping.Dtos;
using Ecommerce.Domain.Shipping;
using FluentValidation;

namespace Ecommerce.Application.Shipping.Validators;

public class SaveCarrierShippingRateRequestValidator : AbstractValidator<SaveCarrierShippingRateRequest>
{
    public SaveCarrierShippingRateRequestValidator()
    {
        RuleFor(x => x.Carrier).IsInEnum();
        RuleFor(x => x.Wilaya)
            .Must(w => AlgerianWilayas.All.Contains(w))
            .WithMessage("Wilaya inconnue.");
        RuleFor(x => x.HomeDeliveryPrice).GreaterThanOrEqualTo(0).When(x => x.HomeDeliveryPrice.HasValue);
        RuleFor(x => x.StopDeskPrice).GreaterThanOrEqualTo(0).When(x => x.StopDeskPrice.HasValue);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
