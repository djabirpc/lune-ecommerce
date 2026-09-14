using Ecommerce.Application.Orders.Dtos;
using FluentValidation;

namespace Ecommerce.Application.Orders.Validators;

public class UpdateOrderNegotiationRequestValidator : AbstractValidator<UpdateOrderNegotiationRequest>
{
    public UpdateOrderNegotiationRequestValidator()
    {
        RuleFor(x => x.ManualDiscountAmount)
            .GreaterThanOrEqualTo(0).WithMessage("La remise négociée ne peut pas être négative.")
            .When(x => x.ManualDiscountAmount.HasValue);
    }
}
