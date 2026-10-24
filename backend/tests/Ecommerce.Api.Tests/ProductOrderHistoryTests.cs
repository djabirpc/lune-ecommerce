using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ecommerce.Application.Auth.Dtos;
using Ecommerce.Application.Catalog.Dtos;
using Ecommerce.Application.Common;
using Ecommerce.Application.Orders.Dtos;
using Ecommerce.Domain.Orders;

namespace Ecommerce.Api.Tests;

public class ProductOrderHistoryTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
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

    private async Task<(ProductDetailDto Product, Guid VariantId, HttpClient AdminClient)> CreateProductWithStockAsync(int quantity, decimal price = 2000m)
    {
        var adminClient = await CreateAuthenticatedClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];

        var categoryResponse = await adminClient.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Cat {unique}", $"cat-{unique}", null, 0));
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>();

        var productResponse = await adminClient.PostAsJsonAsync("/api/products", new CreateProductRequest(
            category!.Id, $"Historique Test {unique}", $"historique-test-{unique}", null, price,
            [new CreateProductVariantRequest("Noir", "M", $"SKU-{unique}", null, quantity)]));
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDetailDto>(JsonOptions);

        return (product!, product!.Variants.Single().Id, adminClient);
    }

    [Fact]
    public async Task GetByProduct_WithoutAuth_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/orders/by-product/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetByProduct_UnknownProduct_ReturnsNotFound()
    {
        var adminClient = await CreateAuthenticatedClientAsync();

        var response = await adminClient.GetAsync($"/api/orders/by-product/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetByProduct_NoOrders_ReturnsEmptyList()
    {
        var (product, _, adminClient) = await CreateProductWithStockAsync(10);

        var response = await adminClient.GetAsync($"/api/orders/by-product/{product.Id}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ProductOrderHistoryItemDto>>(JsonOptions);

        Assert.Empty(result!.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetByProduct_AfterOrderPlaced_ReturnsOrderWithQuantityAndTotal()
    {
        var (product, variantId, adminClient) = await CreateProductWithStockAsync(10, price: 1500m);
        var guestClient = factory.CreateClient();

        var orderRequest = new CreateOrderRequest(
            "Amina", "Benali", "0551234567", "Alger", "Bab Ezzouar", "12 rue des Frères",
            DeliveryType.HomeDelivery, null, [new OrderItemRequest(variantId, 2)]);
        var orderResponse = await guestClient.PostAsJsonAsync("/api/orders", orderRequest, JsonOptions);
        orderResponse.EnsureSuccessStatusCode();
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        var response = await adminClient.GetAsync($"/api/orders/by-product/{product.Id}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ProductOrderHistoryItemDto>>(JsonOptions);

        var match = Assert.Single(result!.Items);
        Assert.Equal(order!.OrderNumber, match.OrderNumber);
        Assert.Equal(2, match.Quantity);
        Assert.Equal(3000m, match.LineTotal);
        Assert.Equal("Amina Benali", match.CustomerFullName);
    }

    [Fact]
    public async Task GetByProduct_MultipleVariantsInSameOrder_SumsQuantityAndTotalIntoOneRow()
    {
        var (product, variantId, adminClient) = await CreateProductWithStockAsync(10, price: 1000m);
        var secondVariantResponse = await adminClient.PostAsJsonAsync(
            $"/api/products/{product.Id}/variants",
            new CreateProductVariantRequest("Rouge", "L", $"SKU-{Guid.NewGuid():N}"[..12], null, 10));
        secondVariantResponse.EnsureSuccessStatusCode();
        var secondVariant = await secondVariantResponse.Content.ReadFromJsonAsync<ProductVariantDto>(JsonOptions);

        var guestClient = factory.CreateClient();
        var orderRequest = new CreateOrderRequest(
            "Amina", "Benali", "0551234567", "Alger", "Bab Ezzouar", "12 rue des Frères",
            DeliveryType.HomeDelivery, null,
            [new OrderItemRequest(variantId, 1), new OrderItemRequest(secondVariant!.Id, 2)]);
        var orderResponse = await guestClient.PostAsJsonAsync("/api/orders", orderRequest, JsonOptions);
        orderResponse.EnsureSuccessStatusCode();

        var response = await adminClient.GetAsync($"/api/orders/by-product/{product.Id}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<ProductOrderHistoryItemDto>>(JsonOptions);

        var match = Assert.Single(result!.Items);
        Assert.Equal(3, match.Quantity); // 1 + 2 across the two variants, one row
        Assert.Equal(3000m, match.LineTotal); // 1x1000 + 2x1000
    }

    [Fact]
    public async Task GetProductStats_WithoutAuth_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/orders/by-product/{Guid.NewGuid()}/stats");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProductStats_UnknownProduct_ReturnsNotFound()
    {
        var adminClient = await CreateAuthenticatedClientAsync();

        var response = await adminClient.GetAsync($"/api/orders/by-product/{Guid.NewGuid()}/stats");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProductStats_NoOrders_ReturnsZeroedStats()
    {
        var (product, _, adminClient) = await CreateProductWithStockAsync(10);

        var response = await adminClient.GetAsync($"/api/orders/by-product/{product.Id}/stats");
        response.EnsureSuccessStatusCode();
        var stats = await response.Content.ReadFromJsonAsync<ProductStatsDto>(JsonOptions);

        Assert.Equal(0, stats!.TotalOrders);
        Assert.Equal(0, stats.TotalUnitsOrdered);
        Assert.Equal(0m, stats.TotalAmount);
        Assert.Empty(stats.ByColor);
        Assert.Empty(stats.BySize);
    }

    [Fact]
    public async Task GetProductStats_MultipleOrdersAcrossColorsAndSizes_AggregatesCorrectly()
    {
        var (product, variantId, adminClient) = await CreateProductWithStockAsync(20, price: 1000m); // Noir/M
        var redLargeResponse = await adminClient.PostAsJsonAsync(
            $"/api/products/{product.Id}/variants",
            new CreateProductVariantRequest("Rouge", "L", $"SKU-{Guid.NewGuid():N}"[..12], null, 20));
        redLargeResponse.EnsureSuccessStatusCode();
        var redLarge = await redLargeResponse.Content.ReadFromJsonAsync<ProductVariantDto>(JsonOptions);

        var guestClient = factory.CreateClient();

        // Order 1: 2x Noir/M + 3x Rouge/L
        await guestClient.PostAsJsonAsync("/api/orders", new CreateOrderRequest(
            "Amina", "Benali", "0551234567", "Alger", "Bab Ezzouar", "12 rue des Frères",
            DeliveryType.HomeDelivery, null,
            [new OrderItemRequest(variantId, 2), new OrderItemRequest(redLarge!.Id, 3)]), JsonOptions);

        // Order 2: 1x Noir/M
        await guestClient.PostAsJsonAsync("/api/orders", new CreateOrderRequest(
            "Sara", "Kaci", "0559876543", "Oran", "Centre", "5 rue du port",
            DeliveryType.HomeDelivery, null,
            [new OrderItemRequest(variantId, 1)]), JsonOptions);

        var response = await adminClient.GetAsync($"/api/orders/by-product/{product.Id}/stats");
        response.EnsureSuccessStatusCode();
        var stats = await response.Content.ReadFromJsonAsync<ProductStatsDto>(JsonOptions);

        Assert.Equal(2, stats!.TotalOrders);
        Assert.Equal(6, stats.TotalUnitsOrdered); // 2 + 3 + 1
        Assert.Equal(6000m, stats.TotalAmount); // 6 x 1000

        var noir = Assert.Single(stats.ByColor, c => c.Value == "Noir");
        Assert.Equal(3, noir.UnitsOrdered); // 2 + 1 across both orders
        var rouge = Assert.Single(stats.ByColor, c => c.Value == "Rouge");
        Assert.Equal(3, rouge.UnitsOrdered);

        var sizeM = Assert.Single(stats.BySize, s => s.Value == "M");
        Assert.Equal(3, sizeM.UnitsOrdered);
        var sizeL = Assert.Single(stats.BySize, s => s.Value == "L");
        Assert.Equal(3, sizeL.UnitsOrdered);
    }
}
