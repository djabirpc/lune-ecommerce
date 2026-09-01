using Ecommerce.Application.Inventory.Dtos;
using FluentValidation;

namespace Ecommerce.Application.Inventory.Validators;

public class AdjustInventoryRequestValidator : AbstractValidator<AdjustInventoryRequest>
{
    public AdjustInventoryRequestValidator()
    {
        RuleFor(x => x.ProductVariantId).NotEmpty();
        RuleFor(x => x.QuantityDelta).NotEqual(0).WithMessage("La quantité d'ajustement ne peut pas être zéro.");
        RuleFor(x => x.Reason).NotEmpty().WithMessage("La raison de l'ajustement est requise.").MaximumLength(500);
    }
}
