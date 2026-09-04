using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ecommerce.Application.Auth.Dtos;
using Ecommerce.Application.Catalog.Dtos;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Inventory;
using Ecommerce.Application.Inventory.Dtos;
using Ecommerce.Application.Suppliers.Dtos;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Api.Tests;

public class CatalogEndpointsTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
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
    public async Task CreateCategory_WithoutAuth_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest("Robes", $"robes-{Guid.NewGuid():N}", null, 0));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProducts_WithoutAuth_ReturnsOk()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/products");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task GetProducts_SortByNewest_ReturnsMostRecentlyCreatedFirst()
    {
        var client = await CreateAuthenticatedClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];

        var categoryResponse = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Cat {unique}", $"cat-{unique}", null, 0));
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>();

        var olderResponse = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            category!.Id, $"Ancien produit {unique}", $"ancien-{unique}", null, 1000m,
            [new CreateProductVariantRequest("Noir", "M", $"SKU-OLD-{unique}", null, 1)]));
        var older = await olderResponse.Content.ReadFromJsonAsync<ProductDetailDto>();

        var newerResponse = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            category.Id, $"Nouveau produit {unique}", $"nouveau-{unique}", null, 1000m,
            [new CreateProductVariantRequest("Noir", "M", $"SKU-NEW-{unique}", null, 1)]));
        var newer = await newerResponse.Content.ReadFromJsonAsync<ProductDetailDto>();

        var response = await client.GetAsync($"/api/products?category={category.Slug}&sortByNewest=true&pageSize=2");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ProductListItemDto>>();

        Assert.Equal(2, result!.Items.Count);
        Assert.Equal(newer!.Id, result.Items[0].Id);
        Assert.Equal(older!.Id, result.Items[1].Id);
    }

    [Fact]
    public async Task FullFlow_CreateCategoryProductAndManageStock_Succeeds()
    {
        var client = await CreateAuthenticatedClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];

        var categoryResponse = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Robes {unique}", $"robes-{unique}", "Des robes.", 0));
        categoryResponse.EnsureSuccessStatusCode();
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>();

        var sku = $"ROBE-{unique}-S";
        var createProductRequest = new CreateProductRequest(
            category!.Id,
            "Robe longue fleurie",
            $"robe-longue-fleurie-{unique}",
            "Une jolie robe.",
            4500m,
            [new CreateProductVariantRequest("Beige", "S", sku, null, 10)]);

        var productResponse = await client.PostAsJsonAsync("/api/products", createProductRequest);
        productResponse.EnsureSuccessStatusCode();
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDetailDto>();
        var variant = product!.Variants.Single();
        Assert.Equal(10, variant.AvailableQuantity);

        var restockResponse = await client.PostAsJsonAsync("/api/inventory/restock", new RestockRequest(variant.Id, 5, "Réassort"));
        restockResponse.EnsureSuccessStatusCode();
        var afterRestock = await restockResponse.Content.ReadFromJsonAsync<InventoryDto>();
        Assert.Equal(15, afterRestock!.AvailableQuantity);

        var adjustDownResponse = await client.PostAsJsonAsync("/api/inventory/adjust", new AdjustInventoryRequest(variant.Id, -3, "Article endommagé trouvé en inventaire"));
        adjustDownResponse.EnsureSuccessStatusCode();
        var afterAdjust = await adjustDownResponse.Content.ReadFromJsonAsync<InventoryDto>();
        Assert.Equal(12, afterAdjust!.AvailableQuantity);

        var overAdjustResponse = await client.PostAsJsonAsync("/api/inventory/adjust", new AdjustInventoryRequest(variant.Id, -1000, "Correction"));
        Assert.Equal(HttpStatusCode.Conflict, overAdjustResponse.StatusCode);

        var productBySlugResponse = await client.GetAsync($"/api/products/{product.Slug}");
        productBySlugResponse.EnsureSuccessStatusCode();
        var fetchedProduct = await productBySlugResponse.Content.ReadFromJsonAsync<ProductDetailDto>();
        Assert.Equal(12, fetchedProduct!.Variants.Single().AvailableQuantity);
    }

    [Fact]
    public async Task InventoryService_Reserve_NeverOversells()
    {
        var client = await CreateAuthenticatedClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];

        var categoryResponse = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Robes {unique}", $"robes-{unique}", null, 0));
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>();

        var productResponse = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            category!.Id,
            "Robe test",
            $"robe-test-{unique}",
            null,
            1000m,
            [new CreateProductVariantRequest("Noir", "M", $"SKU-{unique}", null, 5)]));
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDetailDto>();
        var variantId = product!.Variants.Single().Id;

        using var scope = factory.Services.CreateScope();
        var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryService>();

        await inventoryService.ReserveAsync(variantId, 5);

        await Assert.ThrowsAsync<ConflictAppException>(() => inventoryService.ReserveAsync(variantId, 1));

        var afterFailedReserve = await inventoryService.GetByVariantIdAsync(variantId);
        Assert.Equal(0, afterFailedReserve.AvailableQuantity);
        Assert.Equal(5, afterFailedReserve.ReservedQuantity);

        await inventoryService.ReleaseAsync(variantId, 5);
        var afterRelease = await inventoryService.GetByVariantIdAsync(variantId);
        Assert.Equal(5, afterRelease.AvailableQuantity);
        Assert.Equal(0, afterRelease.ReservedQuantity);
    }

    [Fact]
    public async Task UpdateProduct_ChangesFields_AndIsReflectedOnRefetch()
    {
        var client = await CreateAuthenticatedClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];

        var category1Response = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Robes {unique}", $"robes-{unique}", null, 0));
        var category1 = await category1Response.Content.ReadFromJsonAsync<CategoryDto>();
        var category2Response = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Vestes {unique}", $"vestes-{unique}", null, 0));
        var category2 = await category2Response.Content.ReadFromJsonAsync<CategoryDto>();

        var createResponse = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            category1!.Id, "Robe originale", $"robe-originale-{unique}", null, 2000m,
            [new CreateProductVariantRequest("Noir", "M", $"SKU-{unique}", null, 5)]));
        var product = await createResponse.Content.ReadFromJsonAsync<ProductDetailDto>();
        Assert.Equal(category1.Id, product!.CategoryId);

        var newSlug = $"robe-modifiee-{unique}";
        var updateResponse = await client.PutAsJsonAsync($"/api/products/{product.Id}", new UpdateProductRequest(
            category2!.Id, "Robe modifiée", newSlug, "Nouvelle description", 2500m, false));
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<ProductDetailDto>();

        Assert.Equal("Robe modifiée", updated!.Name);
        Assert.Equal(newSlug, updated.Slug);
        Assert.Equal(category2.Id, updated.CategoryId);
        Assert.Equal(2500m, updated.Price);
        Assert.False(updated.IsActive);

        var refetched = await (await client.GetAsync($"/api/products/{newSlug}")).Content.ReadFromJsonAsync<ProductDetailDto>();
        Assert.Equal("Robe modifiée", refetched!.Name);
        Assert.False(refetched.IsActive);
    }

    [Fact]
    public async Task UpdateProduct_WithDuplicateSlug_ReturnsConflict()
    {
        var client = await CreateAuthenticatedClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];

        var categoryResponse = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Robes {unique}", $"robes-{unique}", null, 0));
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>();

        var firstResponse = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            category!.Id, "Produit A", $"produit-a-{unique}", null, 1000m,
            [new CreateProductVariantRequest("Noir", "M", $"SKU-A-{unique}", null, 1)]));
        var first = await firstResponse.Content.ReadFromJsonAsync<ProductDetailDto>();

        var secondResponse = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            category.Id, "Produit B", $"produit-b-{unique}", null, 1000m,
            [new CreateProductVariantRequest("Noir", "M", $"SKU-B-{unique}", null, 1)]));
        var second = await secondResponse.Content.ReadFromJsonAsync<ProductDetailDto>();

        var updateResponse = await client.PutAsJsonAsync($"/api/products/{second!.Id}", new UpdateProductRequest(
            category.Id, second.Name, first!.Slug, null, second.Price, true));

        Assert.Equal(HttpStatusCode.Conflict, updateResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateCategory_ChangesFields_AndCanDeactivate()
    {
        var client = await CreateAuthenticatedClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];

        var createResponse = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Cat {unique}", $"cat-{unique}", null, 0));
        var category = await createResponse.Content.ReadFromJsonAsync<CategoryDto>();

        var updateResponse = await client.PutAsJsonAsync($"/api/categories/{category!.Id}", new UpdateCategoryRequest(
            "Catégorie renommée", $"cat-renommee-{unique}", "Nouvelle description", false, 5));
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<CategoryDto>();

        Assert.Equal("Catégorie renommée", updated!.Name);
        Assert.Equal($"cat-renommee-{unique}", updated.Slug);
        Assert.False(updated.IsActive);
        Assert.Equal(5, updated.DisplayOrder);

        var listResponse = await client.GetAsync("/api/categories?includeInactive=true");
        var list = await listResponse.Content.ReadFromJsonAsync<List<CategoryDto>>();
        Assert.Contains(list!, c => c.Id == category.Id && !c.IsActive);
    }

    [Fact]
    public async Task UpdateProduct_WithoutAuth_ReturnsUnauthorized()
    {
        var adminClient = await CreateAuthenticatedClientAsync();
        var guestClient = factory.CreateClient();
        var unique = Guid.NewGuid().ToString("N")[..8];

        var categoryResponse = await adminClient.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Robes {unique}", $"robes-{unique}", null, 0));
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>();
        var productResponse = await adminClient.PostAsJsonAsync("/api/products", new CreateProductRequest(
            category!.Id, "Produit test", $"produit-{unique}", null, 1000m,
            [new CreateProductVariantRequest("Noir", "M", $"SKU-{unique}", null, 1)]));
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDetailDto>();

        var response = await guestClient.PutAsJsonAsync($"/api/products/{product!.Id}", new UpdateProductRequest(
            category.Id, product.Name, product.Slug, null, product.Price, true));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Restock_WithSupplierAndUnitCost_UpdatesVariantCostPrice_AndRecordsOnTransaction()
    {
        var client = await CreateAuthenticatedClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];

        var categoryResponse = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Robes {unique}", $"robes-{unique}", null, 0));
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>();

        var supplierResponse = await client.PostAsJsonAsync("/api/suppliers", new SaveSupplierRequest($"Fournisseur {unique}", "0551234567", null, null, null, true));
        var supplier = await supplierResponse.Content.ReadFromJsonAsync<SupplierDto>();

        var productResponse = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            category!.Id, "Robe test", $"robe-cout-{unique}", null, 4500m,
            [new CreateProductVariantRequest("Noir", "M", $"SKU-{unique}", null, 5)]));
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDetailDto>();
        var variantId = product!.Variants.Single().Id;
        Assert.Null(product.Variants.Single().CostPrice);

        var restockResponse = await client.PostAsJsonAsync("/api/inventory/restock", new RestockRequest(
            variantId, 10, "Réassort", supplier!.Id, 2200m));
        restockResponse.EnsureSuccessStatusCode();

        var refetched = await (await client.GetAsync($"/api/products/{product.Slug}")).Content.ReadFromJsonAsync<ProductDetailDto>();
        Assert.Equal(2200m, refetched!.Variants.Single().CostPrice);

        var transactionsResponse = await client.GetAsync($"/api/inventory/{variantId}/transactions");
        transactionsResponse.EnsureSuccessStatusCode();
        var transactions = await transactionsResponse.Content.ReadFromJsonAsync<List<InventoryTransactionDto>>();
        var restockTransaction = transactions!.Single(t => t.Type == "RESTOCK" && t.Quantity == 10);
        Assert.Equal(supplier.Id, restockTransaction.SupplierId);
        Assert.Equal(supplier.Name, restockTransaction.SupplierName);
        Assert.Equal(2200m, restockTransaction.UnitCost);
    }

    [Fact]
    public async Task Restock_WithUnknownSupplier_ReturnsNotFound()
    {
        var client = await CreateAuthenticatedClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];

        var categoryResponse = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Robes {unique}", $"robes-{unique}", null, 0));
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>();
        var productResponse = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            category!.Id, "Robe test", $"robe-{unique}", null, 1000m,
            [new CreateProductVariantRequest("Noir", "M", $"SKU-{unique}", null, 1)]));
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDetailDto>();

        var response = await client.PostAsJsonAsync("/api/inventory/restock", new RestockRequest(
            product!.Variants.Single().Id, 5, null, Guid.NewGuid(), 100m));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_WithPixelIds_PersistsAndReturnsThem()
    {
        var client = await CreateAuthenticatedClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];

        var categoryResponse = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Robes {unique}", $"robes-{unique}", null, 0));
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>();

        var response = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            category!.Id, "Robe pixel", $"robe-pixel-{unique}", null, 1000m,
            [new CreateProductVariantRequest("Noir", "M", $"SKU-{unique}", null, 1)],
            FacebookPixelId: "1234567890123",
            TikTokPixelId: "CXXXXXXXXXXXXXXXXXXX"));
        response.EnsureSuccessStatusCode();
        var product = await response.Content.ReadFromJsonAsync<ProductDetailDto>();

        Assert.Equal("1234567890123", product!.FacebookPixelId);
        Assert.Equal("CXXXXXXXXXXXXXXXXXXX", product.TikTokPixelId);

        var refetched = await (await client.GetAsync($"/api/products/{product.Slug}")).Content.ReadFromJsonAsync<ProductDetailDto>();
        Assert.Equal("1234567890123", refetched!.FacebookPixelId);
    }

    [Fact]
    public async Task UpdateProduct_ChangesPixelIds()
    {
        var client = await CreateAuthenticatedClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];

        var categoryResponse = await client.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Robes {unique}", $"robes-{unique}", null, 0));
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>();
        var createResponse = await client.PostAsJsonAsync("/api/products", new CreateProductRequest(
            category!.Id, "Robe test", $"robe-{unique}", null, 1000m,
            [new CreateProductVariantRequest("Noir", "M", $"SKU-{unique}", null, 1)]));
        var product = await createResponse.Content.ReadFromJsonAsync<ProductDetailDto>();
        Assert.Null(product!.FacebookPixelId);

        var updateResponse = await client.PutAsJsonAsync($"/api/products/{product.Id}", new UpdateProductRequest(
            category.Id, product.Name, product.Slug, null, product.Price, true,
            FacebookPixelId: "999888777666", TikTokPixelId: null));
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<ProductDetailDto>();

        Assert.Equal("999888777666", updated!.FacebookPixelId);
        Assert.Null(updated.TikTokPixelId);
    }
}
