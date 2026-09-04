using Ecommerce.Application.Inventory.Dtos;
using FluentValidation;

namespace Ecommerce.Application.Inventory.Validators;

public class RestockRequestValidator : AbstractValidator<RestockRequest>
{
    public RestockRequestValidator()
    {
        RuleFor(x => x.ProductVariantId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("La quantité doit être supérieure à zéro.");
        RuleFor(x => x.Reason).MaximumLength(500);
        RuleFor(x => x.UnitCost).GreaterThanOrEqualTo(0).When(x => x.UnitCost.HasValue);
    }
}
