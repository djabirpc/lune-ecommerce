namespace Ecommerce.Application.Auth.Dtos;

public record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    CurrentUserResponse User);
