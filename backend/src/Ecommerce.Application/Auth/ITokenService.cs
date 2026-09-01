using System.Security.Claims;

namespace Ecommerce.Application.Auth;

public interface ITokenService
{
    (string Token, DateTime ExpiresAtUtc) GenerateAccessToken(IEnumerable<Claim> claims);

    string GenerateRefreshToken();

    string HashRefreshToken(string refreshToken);
}
