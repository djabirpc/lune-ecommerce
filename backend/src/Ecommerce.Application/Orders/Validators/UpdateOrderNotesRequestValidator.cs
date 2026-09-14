using Ecommerce.Application.Orders.Dtos;
using FluentValidation;

namespace Ecommerce.Application.Orders.Validators;

public class UpdateOrderNotesRequestValidator : AbstractValidator<UpdateOrderNotesRequest>
{
    public UpdateOrderNotesRequestValidator()
    {
        RuleFor(x => x.Notes).MaximumLength(1000).WithMessage("La note ne doit pas dépasser 1000 caractères.");
    }
}
