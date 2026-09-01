using Ecommerce.Application.Catalog.Dtos;
using FluentValidation;

namespace Ecommerce.Application.Catalog.Validators;

public class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).MustBeAValidSlug();
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Variants).NotEmpty().WithMessage("Au moins une variante est requise.");
        RuleForEach(x => x.Variants).SetValidator(new CreateProductVariantRequestValidator());
        RuleFor(x => x.Variants)
            .Must(variants => variants.Select(v => v.Sku).Distinct(StringComparer.OrdinalIgnoreCase).Count() == variants.Count)
            .WithMessage("Les SKU des variantes doivent être uniques.")
            .When(x => x.Variants.Count > 0);
    }
}
