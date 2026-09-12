using Ecommerce.Application.Marketing.Dtos;
using FluentValidation;

namespace Ecommerce.Application.Marketing.Validators;

public class UpdateHomeBannerRequestValidator : AbstractValidator<UpdateHomeBannerRequest>
{
    public UpdateHomeBannerRequestValidator()
    {
        RuleFor(x => x.LinkUrl).MaximumLength(500).WithMessage("Le lien ne doit pas dépasser 500 caractères.");
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0).WithMessage("L'ordre d'affichage doit être positif.");
    }
}
