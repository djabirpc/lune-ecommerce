using Ecommerce.Application.Orders.Dtos;
using Ecommerce.Domain.Orders;
using FluentValidation;

namespace Ecommerce.Application.Orders.Validators;

public class RecordCallAttemptRequestValidator : AbstractValidator<RecordCallAttemptRequest>
{
    public RecordCallAttemptRequestValidator()
    {
        RuleFor(x => x.Result).IsInEnum();
        RuleFor(x => x.Notes).MaximumLength(1000);

        RuleFor(x => x.NextCallAt)
            .NotNull()
            .WithMessage("La date du prochain rappel est requise.")
            .GreaterThan(_ => DateTime.UtcNow)
            .WithMessage("La date du prochain rappel doit être dans le futur.")
            .When(x => x.Result == CallAttemptResult.CallbackScheduled);
    }
}
