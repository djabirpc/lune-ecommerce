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
}
