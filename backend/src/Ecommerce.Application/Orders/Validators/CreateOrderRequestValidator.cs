using System.Text.RegularExpressions;
using Ecommerce.Application.Orders.Dtos;
using FluentValidation;

namespace Ecommerce.Application.Orders.Validators;

public partial class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
{
    public CreateOrderRequestValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("Le prénom est requis.")
            .MaximumLength(100).WithMessage("Le prénom ne doit pas dépasser 100 caractères.");
        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Le nom est requis.")
            .MaximumLength(100).WithMessage("Le nom ne doit pas dépasser 100 caractères.");
        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Le numéro de téléphone est requis.")
            .Matches(AlgerianPhonePattern())
            .WithMessage("Le numéro de téléphone doit être un numéro algérien valide (10 chiffres, commence par 0).");
        RuleFor(x => x.Wilaya)
            .NotEmpty().WithMessage("La wilaya est requise.")
            .MaximumLength(100).WithMessage("La wilaya ne doit pas dépasser 100 caractères.");
        RuleFor(x => x.Commune)
            .NotEmpty().WithMessage("La commune est requise.")
            .MaximumLength(100).WithMessage("La commune ne doit pas dépasser 100 caractères.");
        RuleFor(x => x.Address)
            .NotEmpty().WithMessage("L'adresse de livraison est requise.")
            .MaximumLength(500).WithMessage("L'adresse ne doit pas dépasser 500 caractères.");
        RuleFor(x => x.DeliveryType).IsInEnum().WithMessage("Le type de livraison est invalide.");
        RuleFor(x => x.Notes).MaximumLength(1000).WithMessage("La note ne doit pas dépasser 1000 caractères.");
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
