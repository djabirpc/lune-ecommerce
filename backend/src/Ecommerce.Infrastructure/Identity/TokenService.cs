using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Ecommerce.Application.Auth;
using Ecommerce.Application.Common;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace Ecommerce.Infrastructure.Identity;

public class TokenService(IOptions<JwtOptions> jwtOptions) : ITokenService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;

    public (string Token, DateTime ExpiresAtUtc) GenerateAccessToken(IEnumerable<Claim> claims)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenExpiryMinutes);

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }

    public string HashRefreshToken(string refreshToken)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(hashBytes);
    }
}
