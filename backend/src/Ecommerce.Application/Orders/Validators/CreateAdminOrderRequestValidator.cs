using System.Text.RegularExpressions;
using Ecommerce.Application.Orders.Dtos;
using FluentValidation;

namespace Ecommerce.Application.Orders.Validators;

// Mirrors CreateOrderRequestValidator's field rules by hand (kept in sync manually, same as
// lib/orders/transitions.ts mirroring OrderService.AllowedTransitions) — the two request types aren't
// structurally related so FluentValidation can't share a single rule set across them directly.
public partial class CreateAdminOrderRequestValidator : AbstractValidator<CreateAdminOrderRequest>
{
    public CreateAdminOrderRequestValidator()
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
        RuleFor(x => x.Address).MaximumLength(500).WithMessage("L'adresse ne doit pas dépasser 500 caractères.");
        RuleFor(x => x.DeliveryType).IsInEnum().WithMessage("Le type de livraison est invalide.");
        RuleFor(x => x.Notes).MaximumLength(1000).WithMessage("La note ne doit pas dépasser 1000 caractères.");
        RuleFor(x => x.Items).NotEmpty().WithMessage("La commande doit contenir au moins un article.");
        RuleForEach(x => x.Items).SetValidator(new OrderItemRequestValidator());

        RuleFor(x => x.Items)
            .Must(items => items.Select(i => i.ProductVariantId).Distinct().Count() == items.Count)
            .WithMessage("Chaque variante ne peut apparaître qu'une seule fois dans la commande.")
            .When(x => x.Items.Count > 0);

        RuleFor(x => x.ManualDiscountAmount)
            .GreaterThanOrEqualTo(0).WithMessage("La remise négociée ne peut pas être négative.")
            .When(x => x.ManualDiscountAmount.HasValue);
    }

    [GeneratedRegex(@"^0[0-9]{9}$")]
    private static partial Regex AlgerianPhonePattern();
}
