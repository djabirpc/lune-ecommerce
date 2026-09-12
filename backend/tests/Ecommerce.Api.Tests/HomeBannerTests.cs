using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ecommerce.Application.Auth.Dtos;
using Ecommerce.Application.Marketing.Dtos;

namespace Ecommerce.Api.Tests;

public class HomeBannerTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
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

    private static MultipartFormDataContent BuildUploadContent(string contentType = "image/png", string? linkUrl = null)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent([1, 2, 3, 4]);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "File", "banner.png");
        if (linkUrl is not null)
        {
            content.Add(new StringContent(linkUrl), "LinkUrl");
        }
        return content;
    }

    [Fact]
    public async Task AddBanner_PersistsAndAppearsInActiveList()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsync("/api/home-banners", BuildUploadContent(linkUrl: "/promotions"));
        response.EnsureSuccessStatusCode();
        var banner = await response.Content.ReadFromJsonAsync<HomeBannerDto>();

        Assert.True(banner!.IsActive);
        Assert.Equal("/promotions", banner.LinkUrl);
        Assert.StartsWith("http", banner.ImageUrl);

        var guestClient = factory.CreateClient();
        var active = await (await guestClient.GetAsync("/api/home-banners/active")).Content.ReadFromJsonAsync<List<HomeBannerDto>>();
        Assert.Contains(active!, b => b.Id == banner.Id);
    }

    [Fact]
    public async Task AddBanner_SecondBanner_GetsNextDisplayOrder()
    {
        var client = await CreateAuthenticatedClientAsync();

        var first = await (await client.PostAsync("/api/home-banners", BuildUploadContent())).Content.ReadFromJsonAsync<HomeBannerDto>();
        var second = await (await client.PostAsync("/api/home-banners", BuildUploadContent())).Content.ReadFromJsonAsync<HomeBannerDto>();

        Assert.True(second!.DisplayOrder > first!.DisplayOrder);
    }

    [Fact]
    public async Task UpdateBanner_DeactivatesFromActiveList()
    {
        var client = await CreateAuthenticatedClientAsync();
        var banner = await (await client.PostAsync("/api/home-banners", BuildUploadContent())).Content.ReadFromJsonAsync<HomeBannerDto>();

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/home-banners/{banner!.Id}", new UpdateHomeBannerRequest(null, banner.DisplayOrder, false));
        updateResponse.EnsureSuccessStatusCode();

        var active = await (await factory.CreateClient().GetAsync("/api/home-banners/active")).Content.ReadFromJsonAsync<List<HomeBannerDto>>();
        Assert.DoesNotContain(active!, b => b.Id == banner.Id);
    }

    [Fact]
    public async Task DeleteBanner_RemovesFromAllLists()
    {
        var client = await CreateAuthenticatedClientAsync();
        var banner = await (await client.PostAsync("/api/home-banners", BuildUploadContent())).Content.ReadFromJsonAsync<HomeBannerDto>();

        var deleteResponse = await client.DeleteAsync($"/api/home-banners/{banner!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var all = await (await client.GetAsync("/api/home-banners")).Content.ReadFromJsonAsync<List<HomeBannerDto>>();
        Assert.DoesNotContain(all!, b => b.Id == banner.Id);
    }

    [Fact]
    public async Task AddBanner_WithUnsupportedContentType_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsync("/api/home-banners", BuildUploadContent(contentType: "application/pdf"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AddBanner_WithoutAuth_ReturnsUnauthorized()
    {
        var guestClient = factory.CreateClient();

        var response = await guestClient.PostAsync("/api/home-banners", BuildUploadContent());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetActive_IsPubliclyAccessible()
    {
        var guestClient = factory.CreateClient();

        var response = await guestClient.GetAsync("/api/home-banners/active");

        response.EnsureSuccessStatusCode();
    }
}
