using Ecommerce.Application.Auth.Dtos;

namespace Ecommerce.Application.Auth;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);

    Task LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
}
