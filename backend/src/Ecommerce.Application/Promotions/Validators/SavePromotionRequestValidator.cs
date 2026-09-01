using Ecommerce.Application.Promotions.Dtos;
using Ecommerce.Domain.Promotions;
using FluentValidation;

namespace Ecommerce.Application.Promotions.Validators;

public class SavePromotionRequestValidator : AbstractValidator<SavePromotionRequest>
{
    private static readonly PromotionType[] PercentageTypes =
    [
        PromotionType.ProductDiscount,
        PromotionType.CategoryDiscount,
        PromotionType.FlashSale,
        PromotionType.PercentageDiscount,
    ];

    public SavePromotionRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Priority).GreaterThanOrEqualTo(0);
        RuleFor(x => x.EndsAtUtc).GreaterThan(x => x.StartsAtUtc)
            .WithMessage("La date de fin doit être après la date de début.");

        RuleFor(x => x.PercentageValue)
            .NotNull().WithMessage("Un pourcentage de réduction est requis pour ce type de promotion.")
            .InclusiveBetween(0.01m, 100m).WithMessage("Le pourcentage doit être compris entre 0.01 et 100.")
            .When(x => PercentageTypes.Contains(x.Type));

        RuleFor(x => x.FixedAmountValue)
            .NotNull().WithMessage("Un montant fixe est requis pour ce type de promotion.")
            .GreaterThan(0)
            .When(x => x.Type == PromotionType.FixedAmountDiscount);

        RuleFor(x => x.BuyQuantity)
            .NotNull().WithMessage("La quantité à acheter est requise pour ce type de promotion.")
            .GreaterThan(0)
            .When(x => x.Type == PromotionType.BuyXGetY);

        RuleFor(x => x.GetQuantity)
            .NotNull().WithMessage("La quantité offerte est requise pour ce type de promotion.")
            .GreaterThan(0)
            .When(x => x.Type == PromotionType.BuyXGetY);

        RuleFor(x => x.CouponCode)
            .NotEmpty().WithMessage("Un code promo est requis pour ce type de promotion.")
            .MaximumLength(50)
            .When(x => x.Type == PromotionType.Coupon);

        RuleFor(x => x)
            .Must(x => x.PercentageValue.HasValue ^ x.FixedAmountValue.HasValue)
            .WithMessage("Un code promo doit avoir soit un pourcentage soit un montant fixe (pas les deux).")
            .When(x => x.Type == PromotionType.Coupon);

        RuleFor(x => x.ProductIds)
            .NotEmpty().WithMessage("Sélectionnez au moins un produit pour ce type de promotion.")
            .When(x => x.Type == PromotionType.ProductDiscount);

        RuleFor(x => x.CategoryIds)
            .NotEmpty().WithMessage("Sélectionnez au moins une catégorie pour ce type de promotion.")
            .When(x => x.Type == PromotionType.CategoryDiscount);
    }
}
