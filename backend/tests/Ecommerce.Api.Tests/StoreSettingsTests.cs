using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ecommerce.Application.Auth.Dtos;
using Ecommerce.Application.Marketing.Dtos;

namespace Ecommerce.Api.Tests;

public class StoreSettingsTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
{
    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(
            AuthWebApplicationFactory.AdminEmail,
            AuthWebApplicationFactory.AdminPassword));
        var tokens = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        return client;
    }

    [Fact]
    public async Task Get_WithoutAuth_IsPublicAndDefaultsToNulls()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/store-settings");
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<StoreSettingsDto>();

        Assert.Null(dto!.AnnouncementTextFr);
    }

    [Fact]
    public async Task Put_WithoutAuth_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/store-settings", new UpdateStoreSettingsRequest(
            "https://instagram.com/luna", null, null, "0551234567", "Promo du moment", "عرض اليوم"));

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Put_SetsFields_AndGetReflectsThemPublicly()
    {
        var adminClient = await CreateAuthenticatedClientAsync();

        var putResponse = await adminClient.PutAsJsonAsync("/api/store-settings", new UpdateStoreSettingsRequest(
            "https://instagram.com/luna", "https://tiktok.com/@luna", null, "0551234567", "Promo du moment", "عرض اليوم"));
        putResponse.EnsureSuccessStatusCode();
        var updated = await putResponse.Content.ReadFromJsonAsync<StoreSettingsDto>();

        Assert.Equal("https://instagram.com/luna", updated!.InstagramUrl);
        Assert.Equal("Promo du moment", updated.AnnouncementTextFr);
        Assert.Equal("عرض اليوم", updated.AnnouncementTextAr);
        Assert.Null(updated.FacebookUrl);

        var publicGet = await (await factory.CreateClient().GetAsync("/api/store-settings")).Content.ReadFromJsonAsync<StoreSettingsDto>();
        Assert.Equal("0551234567", publicGet!.ContactPhone);

        // Cleanup so other tests in this shared-DB fixture aren't affected.
        await adminClient.PutAsJsonAsync("/api/store-settings", new UpdateStoreSettingsRequest(null, null, null, null, null, null));
    }

    [Fact]
    public async Task Put_BlankStrings_AreStoredAsNull()
    {
        var adminClient = await CreateAuthenticatedClientAsync();

        await adminClient.PutAsJsonAsync("/api/store-settings", new UpdateStoreSettingsRequest(
            "https://instagram.com/luna", null, null, null, null, null));

        var response = await adminClient.PutAsJsonAsync("/api/store-settings", new UpdateStoreSettingsRequest(
            "   ", null, null, null, null, null));
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<StoreSettingsDto>();

        Assert.Null(dto!.InstagramUrl);
    }
}
