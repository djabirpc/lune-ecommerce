using Ecommerce.Application.Catalog.Dtos;
using FluentValidation;

namespace Ecommerce.Application.Catalog.Validators;

public class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Slug).MustBeAValidSlug();
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}
