using System.Text.RegularExpressions;
using Ecommerce.Application.Orders.Dtos;
using FluentValidation;

namespace Ecommerce.Application.Orders.Validators;

public partial class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone)
            .NotEmpty()
            .Matches(AlgerianPhonePattern())
            .WithMessage("Le numéro de téléphone doit être un numéro algérien valide (10 chiffres, commence par 0).");
        RuleFor(x => x.Wilaya).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Commune).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Address).NotEmpty().MaximumLength(500);
        RuleFor(x => x.DeliveryType).IsInEnum();
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.Items).NotEmpty().WithMessage("La commande doit contenir au moins un article.");
        RuleForEach(x => x.Items).SetValidator(new OrderItemRequestValidator());

        RuleFor(x => x.Items)
            .Must(items => items.Select(i => i.ProductVariantId).Distinct().Count() == items.Count)
            .WithMessage("Chaque variante ne peut apparaître qu'une seule fois dans la commande.")
            .When(x => x.Items.Count > 0);
    }

    [GeneratedRegex(@"^0[0-9]{9}$")]
    private static partial Regex AlgerianPhonePattern();
}
