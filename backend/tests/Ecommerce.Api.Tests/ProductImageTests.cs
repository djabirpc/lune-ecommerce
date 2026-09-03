using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ecommerce.Application.Auth.Dtos;
using Ecommerce.Application.Catalog.Dtos;

namespace Ecommerce.Api.Tests;

public class ProductImageTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
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

    private async Task<ProductDetailDto> CreateProductAsync(HttpClient client)
    {
        var unique = Guid.NewGuid().ToString("N")[..8];

        var categoryResponse = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Cat {unique}", $"cat-{unique}", null, 0));
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>();

        var productResponse = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            category!.Id, "Produit test", $"produit-{unique}", null, 1000m,
            [new CreateProductVariantRequest("Noir", "M", $"SKU-{unique}", null, 1)]));
        return (await productResponse.Content.ReadFromJsonAsync<ProductDetailDto>())!;
    }

    private static MultipartFormDataContent BuildUploadContent(string contentType = "image/png", bool isPrimary = false, string? altText = null)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent([1, 2, 3, 4]);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "File", "test.png");
        content.Add(new StringContent(isPrimary ? "true" : "false"), "IsPrimary");
        if (altText is not null)
        {
            content.Add(new StringContent(altText), "AltText");
        }
        return content;
    }

    [Fact]
    public async Task UploadImage_AsFirstImage_BecomesPrimaryAutomatically()
    {
        var client = await CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(client);

        var response = await client.PostAsync($"/api/products/{product.Id}/images", BuildUploadContent(altText: "Robe noire"));
        response.EnsureSuccessStatusCode();
        var image = await response.Content.ReadFromJsonAsync<ProductImageDto>();

        Assert.True(image!.IsPrimary);
        Assert.Equal("Robe noire", image.AltText);
        Assert.StartsWith("http", image.Url);

        var refetched = await (await client.GetAsync($"/api/products/{product.Slug}")).Content.ReadFromJsonAsync<ProductDetailDto>();
        Assert.Single(refetched!.Images);
        Assert.True(refetched.Images[0].IsPrimary);
    }

    [Fact]
    public async Task UploadSecondImage_WithIsPrimaryTrue_SwapsPrimaryFlag()
    {
        var client = await CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(client);

        var firstResponse = await client.PostAsync($"/api/products/{product.Id}/images", BuildUploadContent());
        var first = await firstResponse.Content.ReadFromJsonAsync<ProductImageDto>();

        var secondResponse = await client.PostAsync($"/api/products/{product.Id}/images", BuildUploadContent(isPrimary: true));
        var second = await secondResponse.Content.ReadFromJsonAsync<ProductImageDto>();

        Assert.True(second!.IsPrimary);

        var refetched = await (await client.GetAsync($"/api/products/{product.Slug}")).Content.ReadFromJsonAsync<ProductDetailDto>();
        Assert.Equal(2, refetched!.Images.Count);
        Assert.False(refetched.Images.Single(i => i.Id == first!.Id).IsPrimary);
        Assert.True(refetched.Images.Single(i => i.Id == second.Id).IsPrimary);
    }

    [Fact]
    public async Task UploadImage_WithUnsupportedContentType_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(client);

        var response = await client.PostAsync($"/api/products/{product.Id}/images", BuildUploadContent(contentType: "application/pdf"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadImage_WithoutAuth_ReturnsUnauthorized()
    {
        var adminClient = await CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(adminClient);
        var guestClient = factory.CreateClient();

        var response = await guestClient.PostAsync($"/api/products/{product.Id}/images", BuildUploadContent());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteImage_WhenPrimary_PromotesNextImageToPrimary()
    {
        var client = await CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(client);

        var firstResponse = await client.PostAsync($"/api/products/{product.Id}/images", BuildUploadContent());
        var first = await firstResponse.Content.ReadFromJsonAsync<ProductImageDto>();
        var secondResponse = await client.PostAsync($"/api/products/{product.Id}/images", BuildUploadContent());
        var second = await secondResponse.Content.ReadFromJsonAsync<ProductImageDto>();

        Assert.True(first!.IsPrimary);
        Assert.False(second!.IsPrimary);

        var deleteResponse = await client.DeleteAsync($"/api/products/{product.Id}/images/{first.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var refetched = await (await client.GetAsync($"/api/products/{product.Slug}")).Content.ReadFromJsonAsync<ProductDetailDto>();
        Assert.Single(refetched!.Images);
        Assert.True(refetched.Images[0].IsPrimary);
        Assert.Equal(second.Id, refetched.Images[0].Id);
    }

    [Fact]
    public async Task SetPrimaryImage_SwapsPrimaryFlagBetweenImages()
    {
        var client = await CreateAuthenticatedClientAsync();
        var product = await CreateProductAsync(client);

        var firstResponse = await client.PostAsync($"/api/products/{product.Id}/images", BuildUploadContent());
        var first = await firstResponse.Content.ReadFromJsonAsync<ProductImageDto>();
        var secondResponse = await client.PostAsync($"/api/products/{product.Id}/images", BuildUploadContent());
        var second = await secondResponse.Content.ReadFromJsonAsync<ProductImageDto>();

        var setPrimaryResponse = await client.PutAsync($"/api/products/{product.Id}/images/{second!.Id}/primary", null);
        setPrimaryResponse.EnsureSuccessStatusCode();
        var updated = await setPrimaryResponse.Content.ReadFromJsonAsync<ProductImageDto>();
        Assert.True(updated!.IsPrimary);

        var refetched = await (await client.GetAsync($"/api/products/{product.Slug}")).Content.ReadFromJsonAsync<ProductDetailDto>();
        Assert.False(refetched!.Images.Single(i => i.Id == first!.Id).IsPrimary);
        Assert.True(refetched.Images.Single(i => i.Id == second.Id).IsPrimary);
    }
}
