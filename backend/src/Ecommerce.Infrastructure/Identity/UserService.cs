using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Users;
using Ecommerce.Application.Users.Dtos;
using Ecommerce.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Identity;

public class UserService(
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext,
    IValidator<CreateUserRequest> createValidator,
    IValidator<UpdateUserRequest> updateValidator,
    IValidator<ResetPasswordRequest> resetPasswordValidator) : IUserService
{
    public async Task<PagedResult<UserDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = userManager.Users.OrderBy(u => u.Email).AsNoTracking();
        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = new List<UserDto>(users.Count);
        foreach (var user in users)
        {
            items.Add(await ToDtoAsync(user));
        }

        return new PagedResult<UserDto>(items, page, pageSize, totalCount);
    }

    public async Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        if (await userManager.FindByEmailAsync(request.Email) is not null)
        {
            throw new ConflictAppException("Un utilisateur avec cet email existe déjà.");
        }

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            EmailConfirmed = true,
            IsActive = true,
        };

        var createResult = await userManager.CreateAsync(user, request.Password);
        if (!createResult.Succeeded)
        {
            throw new ValidationAppException(string.Join(" ", createResult.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRolesAsync(user, request.Roles);

        return await ToDtoAsync(user);
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, Guid currentUserId, CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var user = await userManager.FindByIdAsync(id.ToString())
            ?? throw new NotFoundAppException("Utilisateur introuvable.");

        if (id == currentUserId && !request.IsActive)
        {
            throw new ConflictAppException("Vous ne pouvez pas désactiver votre propre compte.");
        }

        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.IsActive = request.IsActive;

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            throw new ValidationAppException(string.Join(" ", updateResult.Errors.Select(e => e.Description)));
        }

        var currentRoles = await userManager.GetRolesAsync(user);
        var rolesToRemove = currentRoles.Except(request.Roles).ToList();
        var rolesToAdd = request.Roles.Except(currentRoles).ToList();

        if (rolesToRemove.Count > 0)
        {
            await userManager.RemoveFromRolesAsync(user, rolesToRemove);
        }
        if (rolesToAdd.Count > 0)
        {
            await userManager.AddToRolesAsync(user, rolesToAdd);
        }

        return await ToDtoAsync(user);
    }

    public async Task ResetPasswordAsync(Guid id, ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        await resetPasswordValidator.ValidateAndThrowAsync(request, cancellationToken);

        var user = await userManager.FindByIdAsync(id.ToString())
            ?? throw new NotFoundAppException("Utilisateur introuvable.");

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await userManager.ResetPasswordAsync(user, token, request.NewPassword);
        if (!resetResult.Succeeded)
        {
            throw new ValidationAppException(string.Join(" ", resetResult.Errors.Select(e => e.Description)));
        }

        var activeTokens = await dbContext.RefreshTokens
            .Where(rt => rt.UserId == id && rt.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);
        foreach (var refreshToken in activeTokens)
        {
            refreshToken.RevokedAtUtc = DateTime.UtcNow;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<UserDto> ToDtoAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new UserDto(user.Id, user.Email ?? string.Empty, user.FirstName, user.LastName, user.IsActive, roles.ToList(), user.CreatedAtUtc);
    }
}
