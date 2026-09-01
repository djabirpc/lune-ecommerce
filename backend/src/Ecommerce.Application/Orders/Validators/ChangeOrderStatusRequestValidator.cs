using Ecommerce.Application.Orders.Dtos;
using FluentValidation;

namespace Ecommerce.Application.Orders.Validators;

public class ChangeOrderStatusRequestValidator : AbstractValidator<ChangeOrderStatusRequest>
{
    public ChangeOrderStatusRequestValidator()
    {
        RuleFor(x => x.NewStatus).IsInEnum();
        RuleFor(x => x.Reason).MaximumLength(500);
    }
}
