using Ecommerce.Application.Orders.Dtos;
using FluentValidation;

namespace Ecommerce.Application.Orders.Validators;

public class AddOrderItemRequestValidator : AbstractValidator<AddOrderItemRequest>
{
    public AddOrderItemRequestValidator()
    {
        RuleFor(x => x.ProductVariantId).NotEmpty();
        RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("La quantité doit être supérieure à zéro.");
    }
}
