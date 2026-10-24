using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ecommerce.Application.Auth.Dtos;
using Ecommerce.Application.Catalog.Dtos;
using Ecommerce.Application.Common;
using Ecommerce.Application.Inventory.Dtos;

namespace Ecommerce.Api.Tests;

public class InventoryOverviewTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

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

    private async Task<(ProductDetailDto Product, HttpClient AdminClient)> CreateProductWithVariantAsync(int stock, string? uniqueOverride = null)
    {
        var adminClient = await CreateAuthenticatedClientAsync();
        var unique = uniqueOverride ?? Guid.NewGuid().ToString("N")[..8];

        var categoryResponse = await adminClient.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Cat {unique}", $"cat-{unique}", null, 0));
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>();

        var productResponse = await adminClient.PostAsJsonAsync("/api/products", new CreateProductRequest(
            category!.Id,
            $"Inventaire Test {unique}",
            $"inventaire-test-{unique}",
            null,
            2000m,
            [new CreateProductVariantRequest("Noir", "M", $"SKU-{unique}", null, stock)]));
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDetailDto>(JsonOptions);

        return (product!, adminClient);
    }

    [Fact]
    public async Task GetOverview_WithoutAuth_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/inventory");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOverview_SearchBySku_FindsExactProductWithAllItsVariants()
    {
        var (product, adminClient) = await CreateProductWithVariantAsync(10);
        var sku = product.Variants.Single().Sku;

        var response = await adminClient.GetAsync($"/api/inventory?search={sku}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<InventoryProductOverviewDto>>(JsonOptions);

        var match = Assert.Single(result!.Items);
        Assert.Equal(product.Id, match.ProductId);
        Assert.Contains(match.Variants, v => v.Sku == sku);
    }

    [Fact]
    public async Task GetOverview_StockStatusOut_OnlyReturnsProductsWithAZeroStockVariant()
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (outOfStockProduct, adminClient) = await CreateProductWithVariantAsync(0, $"out-{unique}");
        var (inStockProduct, _) = await CreateProductWithVariantAsync(20, $"in-{unique}");

        var response = await adminClient.GetAsync("/api/inventory?search=Inventaire Test&stockStatus=Out&pageSize=100");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<InventoryProductOverviewDto>>(JsonOptions);

        Assert.Contains(result!.Items, p => p.ProductId == outOfStockProduct.Id);
        Assert.DoesNotContain(result.Items, p => p.ProductId == inStockProduct.Id);
    }

    [Fact]
    public async Task GetOverview_StockStatusLow_OnlyReturnsProductsWithALowButNonZeroStockVariant()
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        var (lowStockProduct, adminClient) = await CreateProductWithVariantAsync(3, $"low-{unique}");
        var (highStockProduct, _) = await CreateProductWithVariantAsync(50, $"high-{unique}");

        var response = await adminClient.GetAsync("/api/inventory?stockStatus=Low&pageSize=100");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<InventoryProductOverviewDto>>(JsonOptions);

        Assert.Contains(result!.Items, p => p.ProductId == lowStockProduct.Id);
        Assert.DoesNotContain(result.Items, p => p.ProductId == highStockProduct.Id);
    }

    [Fact]
    public async Task GetOverview_FilterByCategory_OnlyReturnsThatCategorysProducts()
    {
        var (product, adminClient) = await CreateProductWithVariantAsync(10);

        var response = await adminClient.GetAsync($"/api/inventory?categoryId={product.CategoryId}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<InventoryProductOverviewDto>>(JsonOptions);

        Assert.All(result!.Items, p => Assert.Equal(product.CategoryId, p.CategoryId));
        Assert.Contains(result.Items, p => p.ProductId == product.Id);
    }

    [Fact]
    public async Task GetOverview_RestockThroughExistingEndpoint_IsReflectedInOverview()
    {
        var (product, adminClient) = await CreateProductWithVariantAsync(5);
        var variantId = product.Variants.Single().Id;

        var restockResponse = await adminClient.PostAsJsonAsync(
            "/api/inventory/restock",
            new { productVariantId = variantId, quantity = 10, reason = "Test" });
        restockResponse.EnsureSuccessStatusCode();

        var response = await adminClient.GetAsync($"/api/inventory?search={product.Variants.Single().Sku}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<InventoryProductOverviewDto>>(JsonOptions);

        Assert.Equal(15, result!.Items.Single().Variants.Single().AvailableQuantity);
    }

    [Fact]
    public async Task GetByProduct_ReturnsProductWithAllItsVariants()
    {
        var (product, adminClient) = await CreateProductWithVariantAsync(10);

        var secondVariantResponse = await adminClient.PostAsJsonAsync(
            $"/api/products/{product.Id}/variants",
            new CreateProductVariantRequest("Rouge", "L", $"SKU-{Guid.NewGuid():N}"[..12], null, 3));
        secondVariantResponse.EnsureSuccessStatusCode();

        var response = await adminClient.GetAsync($"/api/inventory/by-product/{product.Id}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<InventoryProductOverviewDto>(JsonOptions);

        Assert.Equal(product.Id, result!.ProductId);
        Assert.Equal(2, result.Variants.Count);
    }

    [Fact]
    public async Task GetByProduct_UnknownProduct_ReturnsNotFound()
    {
        var adminClient = await CreateAuthenticatedClientAsync();

        var response = await adminClient.GetAsync($"/api/inventory/by-product/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetOverview_ProductWithMultipleVariants_ReturnsAllVariantsTogether()
    {
        var (product, adminClient) = await CreateProductWithVariantAsync(10);

        var secondVariantResponse = await adminClient.PostAsJsonAsync(
            $"/api/products/{product.Id}/variants",
            new CreateProductVariantRequest("Rouge", "L", $"SKU-{Guid.NewGuid():N}"[..12], null, 0));
        secondVariantResponse.EnsureSuccessStatusCode();

        var response = await adminClient.GetAsync($"/api/inventory?search={product.Name}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<InventoryProductOverviewDto>>(JsonOptions);

        var match = Assert.Single(result!.Items);
        Assert.Equal(2, match.Variants.Count);
    }
}
