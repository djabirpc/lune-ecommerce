using Ecommerce.Application.Auth.Dtos;
using FluentValidation;

namespace Ecommerce.Application.Auth.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("L'adresse e-mail est requise.")
            .EmailAddress().WithMessage("L'adresse e-mail n'est pas valide.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Le mot de passe est requis.");
    }
}
