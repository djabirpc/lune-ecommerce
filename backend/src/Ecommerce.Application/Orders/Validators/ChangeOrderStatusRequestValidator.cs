using Ecommerce.Application.Orders.Dtos;
using Ecommerce.Domain.Orders;
using FluentValidation;

namespace Ecommerce.Application.Orders.Validators;

public class ChangeOrderStatusRequestValidator : AbstractValidator<ChangeOrderStatusRequest>
{
    public ChangeOrderStatusRequestValidator()
    {
        RuleFor(x => x.NewStatus).IsInEnum();
        RuleFor(x => x.Reason).MaximumLength(500);
        RuleFor(x => x.ReturnReason).IsInEnum().When(x => x.ReturnReason.HasValue);
        RuleFor(x => x.ReturnReason)
            .NotNull()
            .WithMessage("La cause du retour est requise.")
            .When(x => x.NewStatus == OrderStatus.Returned);
    }
}
