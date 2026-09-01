using Ecommerce.Application.Catalog.Dtos;
using FluentValidation;

namespace Ecommerce.Application.Catalog.Validators;

public class CreateProductVariantRequestValidator : AbstractValidator<CreateProductVariantRequest>
{
    public CreateProductVariantRequestValidator()
    {
        RuleFor(x => x.Color).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Size).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Sku).NotEmpty().MaximumLength(64);
        RuleFor(x => x.PriceOverride).GreaterThan(0).When(x => x.PriceOverride.HasValue);
        RuleFor(x => x.InitialQuantity).GreaterThanOrEqualTo(0);
    }
}
