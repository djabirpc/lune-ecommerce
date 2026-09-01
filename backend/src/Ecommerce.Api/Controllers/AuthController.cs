using System.Security.Claims;
using Ecommerce.Application.Auth;
using Ecommerce.Application.Auth.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.RefreshAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public ActionResult<CurrentUserResponse> Me()
    {
        var id = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var firstName = User.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty;
        var lastName = User.FindFirstValue(ClaimTypes.Surname) ?? string.Empty;
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

        return Ok(new CurrentUserResponse(id, email, firstName, lastName, roles));
    }
}
