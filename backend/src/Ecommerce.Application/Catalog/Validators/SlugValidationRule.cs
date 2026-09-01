using System.Text.RegularExpressions;
using FluentValidation;

namespace Ecommerce.Application.Catalog.Validators;

public static partial class SlugValidationRule
{
    public static IRuleBuilderOptions<T, string> MustBeAValidSlug<T>(this IRuleBuilder<T, string> ruleBuilder) =>
        ruleBuilder
            .NotEmpty().WithMessage("Le slug est requis.")
            .MaximumLength(220).WithMessage("Le slug ne doit pas dépasser 220 caractères.")
            .Matches(SlugPattern()).WithMessage("Le slug doit être en minuscules, alphanumérique, avec des tirets (ex: robe-longue-fleurie).");

    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial Regex SlugPattern();
}
