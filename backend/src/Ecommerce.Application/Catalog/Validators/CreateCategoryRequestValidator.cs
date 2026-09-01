using Ecommerce.Application.Catalog.Dtos;
using FluentValidation;

namespace Ecommerce.Application.Catalog.Validators;

public class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Slug).MustBeAValidSlug();
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
