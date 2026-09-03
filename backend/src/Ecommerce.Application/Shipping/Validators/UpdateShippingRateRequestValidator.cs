using Ecommerce.Application.Shipping.Dtos;
using FluentValidation;

namespace Ecommerce.Application.Shipping.Validators;

public class UpdateShippingRateRequestValidator : AbstractValidator<UpdateShippingRateRequest>
{
    public UpdateShippingRateRequestValidator()
    {
        RuleFor(x => x.HomeDeliveryPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.StopDeskPrice).GreaterThanOrEqualTo(0);
    }
}
