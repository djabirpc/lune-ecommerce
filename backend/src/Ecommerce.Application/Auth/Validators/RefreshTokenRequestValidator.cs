using Ecommerce.Application.Auth.Dtos;
using FluentValidation;

namespace Ecommerce.Application.Auth.Validators;

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage("Le refresh token est requis.");
    }
}
