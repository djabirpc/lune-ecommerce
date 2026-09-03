using Ecommerce.Application.Users.Dtos;
using Ecommerce.Domain.Identity;
using FluentValidation;

namespace Ecommerce.Application.Users.Validators;

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Roles)
            .NotEmpty().WithMessage("Au moins un rôle est requis.")
            .Must(roles => roles.All(Roles.All.Contains)).WithMessage("Un ou plusieurs rôles sont invalides.");
    }
}
