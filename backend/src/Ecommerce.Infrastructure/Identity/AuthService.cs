using System.Security.Claims;
using Ecommerce.Application.Auth;
using Ecommerce.Application.Auth.Dtos;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Ecommerce.Infrastructure.Identity;

public class AuthService(
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext,
    ITokenService tokenService,
    IOptions<JwtOptions> jwtOptions,
    IValidator<LoginRequest> loginValidator,
    IValidator<RefreshTokenRequest> refreshTokenValidator) : IAuthService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;
    private const string InvalidCredentialsMessage = "Email ou mot de passe incorrect.";
    private const string InvalidRefreshTokenMessage = "Refresh token invalide ou expiré.";

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        await loginValidator.ValidateAndThrowAsync(request, cancellationToken);

        var user = await userManager.FindByEmailAsync(request.Email);

        if (user is null || !user.IsActive || !await userManager.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedAppException(InvalidCredentialsMessage);
        }

        return await IssueTokensAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        await refreshTokenValidator.ValidateAndThrowAsync(request, cancellationToken);

        var tokenHash = tokenService.HashRefreshToken(request.RefreshToken);

        var storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null || !storedToken.IsActive)
        {
            throw new UnauthorizedAppException(InvalidRefreshTokenMessage);
        }

        var user = await userManager.FindByIdAsync(storedToken.UserId.ToString());

        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAppException(InvalidRefreshTokenMessage);
        }

        var response = await IssueTokensAsync(user, cancellationToken);

        storedToken.RevokedAtUtc = DateTime.UtcNow;
        storedToken.ReplacedByTokenHash = tokenService.HashRefreshToken(response.RefreshToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        await refreshTokenValidator.ValidateAndThrowAsync(request, cancellationToken);

        var tokenHash = tokenService.HashRefreshToken(request.RefreshToken);

        var storedToken = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (storedToken is not null && storedToken.IsActive)
        {
            storedToken.RevokedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<AuthResponse> IssueTokensAsync(ApplicationUser user, CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.GivenName, user.FirstName),
            new(ClaimTypes.Surname, user.LastName),
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var (accessToken, accessTokenExpiresAtUtc) = tokenService.GenerateAccessToken(claims);
        var refreshToken = tokenService.GenerateRefreshToken();
        var refreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenExpiryDays);

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokenService.HashRefreshToken(refreshToken),
            ExpiresAtUtc = refreshTokenExpiresAtUtc,
        });
        await dbContext.SaveChangesAsync(cancellationToken);

        var currentUser = new CurrentUserResponse(
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            roles.ToList());

        return new AuthResponse(
            accessToken,
            accessTokenExpiresAtUtc,
            refreshToken,
            refreshTokenExpiresAtUtc,
            currentUser);
    }
}
