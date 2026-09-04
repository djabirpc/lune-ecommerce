using Ecommerce.Application.Catalog.Dtos;
using FluentValidation;

namespace Ecommerce.Application.Catalog.Validators;

public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.CategoryId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).MustBeAValidSlug();
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.FacebookPixelId).MaximumLength(50);
        RuleFor(x => x.TikTokPixelId).MaximumLength(50);
    }
}
