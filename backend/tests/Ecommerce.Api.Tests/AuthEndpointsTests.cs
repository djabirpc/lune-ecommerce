using System.Net;
using System.Net.Http.Json;
using Ecommerce.Application.Auth.Dtos;

namespace Ecommerce.Api.Tests;

public class AuthEndpointsTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
{
    [Fact]
    public async Task Login_WithSeededAdminCredentials_ReturnsTokens()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(
            AuthWebApplicationFactory.AdminEmail,
            AuthWebApplicationFactory.AdminPassword));

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();

        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(body.RefreshToken));
        Assert.Contains("SUPER_ADMIN", body.User.Roles);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorizedWithStandardErrorShape()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(
            AuthWebApplicationFactory.AdminEmail,
            "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorEnvelope>();
        Assert.NotNull(body);
        Assert.False(body!.Success);
        Assert.Equal("UNAUTHORIZED", body.Error.Code);
    }

    [Fact]
    public async Task Me_WithoutToken_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task FullFlow_LoginThenMeThenRefreshThenLogout_Succeeds()
    {
        var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(
            AuthWebApplicationFactory.AdminEmail,
            AuthWebApplicationFactory.AdminPassword));
        var tokens = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(tokens);

        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        var meResponse = await client.GetAsync("/api/auth/me");
        meResponse.EnsureSuccessStatusCode();
        var me = await meResponse.Content.ReadFromJsonAsync<CurrentUserResponse>();
        Assert.Equal(AuthWebApplicationFactory.AdminEmail, me!.Email);

        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(tokens.RefreshToken));
        refreshResponse.EnsureSuccessStatusCode();
        var refreshed = await refreshResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(refreshed);
        Assert.NotEqual(tokens.RefreshToken, refreshed!.RefreshToken);

        var logoutResponse = await client.PostAsJsonAsync("/api/auth/logout", new RefreshTokenRequest(refreshed.RefreshToken));
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var reuseRefreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(refreshed.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, reuseRefreshResponse.StatusCode);
    }

    private record ErrorEnvelope(bool Success, ErrorDetail Error);
    private record ErrorDetail(string Code, string Message);
}
