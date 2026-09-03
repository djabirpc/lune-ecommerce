using Ecommerce.Application.Users.Dtos;
using FluentValidation;

namespace Ecommerce.Application.Users.Validators;

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8);
    }
}
