using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ecommerce.Application.Auth.Dtos;
using Ecommerce.Application.Users.Dtos;

namespace Ecommerce.Api.Tests;

public class UsersTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
{
    private async Task<(HttpClient Client, CurrentUserResponse Me)> CreateAuthenticatedClientAsync()
    {
        var client = factory.CreateClient();

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(
            AuthWebApplicationFactory.AdminEmail,
            AuthWebApplicationFactory.AdminPassword));
        var tokens = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);

        var meResponse = await client.GetAsync("/api/auth/me");
        var me = await meResponse.Content.ReadFromJsonAsync<CurrentUserResponse>();

        return (client, me!);
    }

    [Fact]
    public async Task CreateUser_WithValidData_CreatesAndReturnsUserWithRoles()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];

        var response = await client.PostAsJsonAsync("/api/users", new CreateUserRequest(
            $"agent-{unique}@luna.test", "Password-123!", "Amina", "Kaci", ["CONFIRMATION_AGENT"]));
        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<UserDto>();

        Assert.Equal($"agent-{unique}@luna.test", user!.Email);
        Assert.True(user.IsActive);
        Assert.Single(user.Roles);
        Assert.Equal("CONFIRMATION_AGENT", user.Roles[0]);
    }

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_ReturnsConflict()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"dup-{unique}@luna.test";

        var first = await client.PostAsJsonAsync("/api/users", new CreateUserRequest(email, "Password-123!", "A", "B", ["VIEWER"]));
        first.EnsureSuccessStatusCode();

        var second = await client.PostAsJsonAsync("/api/users", new CreateUserRequest(email, "Password-123!", "C", "D", ["VIEWER"]));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WithInvalidRole_ReturnsBadRequest()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];

        var response = await client.PostAsJsonAsync("/api/users", new CreateUserRequest(
            $"bad-{unique}@luna.test", "Password-123!", "A", "B", ["NOT_A_REAL_ROLE"]));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_ChangesRolesAndCanDeactivate()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];

        var createResponse = await client.PostAsJsonAsync("/api/users", new CreateUserRequest(
            $"update-{unique}@luna.test", "Password-123!", "Amina", "Kaci", ["VIEWER"]));
        var created = await createResponse.Content.ReadFromJsonAsync<UserDto>();

        var updateResponse = await client.PutAsJsonAsync($"/api/users/{created!.Id}", new UpdateUserRequest(
            "Amina", "Benali", false, ["ORDER_MANAGER", "STOCK_MANAGER"]));
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<UserDto>();

        Assert.Equal("Benali", updated!.LastName);
        Assert.False(updated.IsActive);
        Assert.Equal(2, updated.Roles.Count);
        Assert.Contains("ORDER_MANAGER", updated.Roles);
        Assert.Contains("STOCK_MANAGER", updated.Roles);
        Assert.DoesNotContain("VIEWER", updated.Roles);
    }

    [Fact]
    public async Task DeactivatedUser_CannotLogin()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"deactivated-{unique}@luna.test";

        var createResponse = await client.PostAsJsonAsync("/api/users", new CreateUserRequest(
            email, "Password-123!", "A", "B", ["VIEWER"]));
        var created = await createResponse.Content.ReadFromJsonAsync<UserDto>();

        await client.PutAsJsonAsync($"/api/users/{created!.Id}", new UpdateUserRequest("A", "B", false, ["VIEWER"]));

        var guestClient = factory.CreateClient();
        var loginResponse = await guestClient.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Password-123!"));

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_CannotDeactivateOwnAccount()
    {
        var (client, me) = await CreateAuthenticatedClientAsync();

        var response = await client.PutAsJsonAsync($"/api/users/{me.Id}", new UpdateUserRequest(
            me.FirstName, me.LastName, false, me.Roles));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetPaged_WithoutAuth_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_AllowsLoginWithNewPassword_AndRejectsOldPassword()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"reset-{unique}@luna.test";

        var createResponse = await client.PostAsJsonAsync("/api/users", new CreateUserRequest(
            email, "Old-Password-123!", "A", "B", ["VIEWER"]));
        var created = await createResponse.Content.ReadFromJsonAsync<UserDto>();

        var resetResponse = await client.PostAsJsonAsync($"/api/users/{created!.Id}/reset-password", new ResetPasswordRequest("New-Password-456!"));
        Assert.Equal(HttpStatusCode.NoContent, resetResponse.StatusCode);

        var guestClient = factory.CreateClient();
        var oldPasswordLogin = await guestClient.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Old-Password-123!"));
        Assert.Equal(HttpStatusCode.Unauthorized, oldPasswordLogin.StatusCode);

        var newPasswordLogin = await guestClient.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "New-Password-456!"));
        newPasswordLogin.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task ResetPassword_RevokesExistingRefreshTokens()
    {
        var (adminClient, _) = await CreateAuthenticatedClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var email = $"revoke-{unique}@luna.test";

        var createResponse = await adminClient.PostAsJsonAsync("/api/users", new CreateUserRequest(
            email, "Old-Password-123!", "A", "B", ["VIEWER"]));
        var created = await createResponse.Content.ReadFromJsonAsync<UserDto>();

        var staffClient = factory.CreateClient();
        var staffLogin = await staffClient.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Old-Password-123!"));
        var staffTokens = await staffLogin.Content.ReadFromJsonAsync<AuthResponse>();

        await adminClient.PostAsJsonAsync($"/api/users/{created!.Id}/reset-password", new ResetPasswordRequest("New-Password-456!"));

        var refreshAttempt = await staffClient.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(staffTokens!.RefreshToken));

        Assert.Equal(HttpStatusCode.Unauthorized, refreshAttempt.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_TooShort_ReturnsBadRequest()
    {
        var (client, _) = await CreateAuthenticatedClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];

        var createResponse = await client.PostAsJsonAsync("/api/users", new CreateUserRequest(
            $"short-{unique}@luna.test", "Old-Password-123!", "A", "B", ["VIEWER"]));
        var created = await createResponse.Content.ReadFromJsonAsync<UserDto>();

        var response = await client.PostAsJsonAsync($"/api/users/{created!.Id}/reset-password", new ResetPasswordRequest("short"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResetPassword_WithoutAuth_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync($"/api/users/{Guid.NewGuid()}/reset-password", new ResetPasswordRequest("New-Password-456!"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
