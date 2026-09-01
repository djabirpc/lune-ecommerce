using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ecommerce.Application.Auth.Dtos;
using Ecommerce.Application.Catalog.Dtos;
using Ecommerce.Application.Orders.Dtos;
using Ecommerce.Application.Promotions.Dtos;
using Ecommerce.Domain.Orders;
using Ecommerce.Domain.Promotions;

namespace Ecommerce.Api.Tests;

public class PromotionCheckoutTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
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

    private async Task<(Guid VariantId, Guid ProductId, Guid CategoryId, HttpClient AdminClient)> CreateProductWithStockAsync(int initialQuantity, decimal price = 2000m)
    {
        var adminClient = await CreateAuthenticatedClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];

        var categoryResponse = await adminClient.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Cat {unique}", $"cat-{unique}", null, 0));
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>();

        var productResponse = await adminClient.PostAsJsonAsync("/api/products", new CreateProductRequest(
            category!.Id,
            "Produit test",
            $"produit-{unique}",
            null,
            price,
            [new CreateProductVariantRequest("Noir", "M", $"SKU-{unique}", null, initialQuantity)]));
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDetailDto>();

        return (product!.Variants.Single().Id, product.Id, category.Id, adminClient);
    }

    private static CreateOrderRequest BuildOrderRequest(Guid variantId, int quantity, string? couponCode = null) => new(
        "Amina",
        "Benali",
        "0551234567",
        "Alger",
        "Bab Ezzouar",
        "12 rue des Frères",
        DeliveryType.HomeDelivery,
        null,
        [new OrderItemRequest(variantId, quantity)],
        couponCode);

    private async Task<PromotionDetailDto> CreatePromotionAsync(HttpClient adminClient, SavePromotionRequest request)
    {
        var response = await adminClient.PostAsJsonAsync("/api/promotions", request, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PromotionDetailDto>(JsonOptions))!;
    }

    [Fact]
    public async Task ProductPercentageDiscount_ReducesOrderTotal()
    {
        var (variantId, productId, _, adminClient) = await CreateProductWithStockAsync(10, price: 1000m);
        var guestClient = factory.CreateClient();

        await CreatePromotionAsync(adminClient, new SavePromotionRequest(
            "Promo produit",
            null,
            PromotionType.ProductDiscount,
            10m,
            null,
            null,
            null,
            null,
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow.AddDays(1),
            true,
            0,
            [productId],
            []));

        var response = await guestClient.PostAsJsonAsync("/api/orders", BuildOrderRequest(variantId, 2), JsonOptions);
        response.EnsureSuccessStatusCode();
        var order = await response.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        Assert.Equal(2000m, order!.Subtotal);
        Assert.Equal(200m, order.DiscountTotal);
        Assert.Equal(1800m, order.Total);
        Assert.Single(order.AppliedPromotions);
    }

    [Fact]
    public async Task CategoryDiscount_AppliesToProductInThatCategory()
    {
        var (variantId, _, categoryId, adminClient) = await CreateProductWithStockAsync(10, price: 1000m);
        var guestClient = factory.CreateClient();

        await CreatePromotionAsync(adminClient, new SavePromotionRequest(
            "Promo catégorie",
            null,
            PromotionType.CategoryDiscount,
            15m,
            null,
            null,
            null,
            null,
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow.AddDays(1),
            true,
            0,
            [],
            [categoryId]));

        var response = await guestClient.PostAsJsonAsync("/api/orders", BuildOrderRequest(variantId, 1), JsonOptions);
        response.EnsureSuccessStatusCode();
        var order = await response.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        Assert.Equal(150m, order!.DiscountTotal);
        Assert.Equal(850m, order.Total);
    }

    [Fact]
    public async Task ValidCoupon_AppliesDiscount()
    {
        var (variantId, _, _, adminClient) = await CreateProductWithStockAsync(10, price: 1000m);
        var guestClient = factory.CreateClient();
        var couponCode = $"SAVE10-{Guid.NewGuid():N}"[..12].ToUpperInvariant();

        await CreatePromotionAsync(adminClient, new SavePromotionRequest(
            "Coupon test",
            null,
            PromotionType.Coupon,
            null,
            100m,
            null,
            null,
            couponCode,
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow.AddDays(1),
            true,
            0,
            [],
            []));

        var response = await guestClient.PostAsJsonAsync("/api/orders", BuildOrderRequest(variantId, 1, couponCode), JsonOptions);
        response.EnsureSuccessStatusCode();
        var order = await response.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        Assert.Equal(100m, order!.DiscountTotal);
        Assert.Equal(900m, order.Total);
    }

    [Fact]
    public async Task InvalidCouponCode_ReturnsBadRequest()
    {
        var (variantId, _, _, _) = await CreateProductWithStockAsync(10, price: 1000m);
        var guestClient = factory.CreateClient();

        var response = await guestClient.PostAsJsonAsync("/api/orders", BuildOrderRequest(variantId, 1, "DOES-NOT-EXIST"), JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ExpiredPromotion_DoesNotApply()
    {
        var (variantId, productId, _, adminClient) = await CreateProductWithStockAsync(10, price: 1000m);
        var guestClient = factory.CreateClient();

        await CreatePromotionAsync(adminClient, new SavePromotionRequest(
            "Promo expirée",
            null,
            PromotionType.ProductDiscount,
            50m,
            null,
            null,
            null,
            null,
            DateTime.UtcNow.AddDays(-10),
            DateTime.UtcNow.AddDays(-1),
            true,
            0,
            [productId],
            []));

        var response = await guestClient.PostAsJsonAsync("/api/orders", BuildOrderRequest(variantId, 1), JsonOptions);
        response.EnsureSuccessStatusCode();
        var order = await response.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        Assert.Equal(0m, order!.DiscountTotal);
        Assert.Equal(1000m, order.Total);
    }

    [Fact]
    public async Task HighestPriorityPromotion_WinsWhenMultipleMatch()
    {
        var (variantId, productId, categoryId, adminClient) = await CreateProductWithStockAsync(10, price: 1000m);
        var guestClient = factory.CreateClient();

        await CreatePromotionAsync(adminClient, new SavePromotionRequest(
            "Basse priorité",
            null,
            PromotionType.CategoryDiscount,
            5m,
            null,
            null,
            null,
            null,
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow.AddDays(1),
            true,
            0,
            [],
            [categoryId]));

        await CreatePromotionAsync(adminClient, new SavePromotionRequest(
            "Haute priorité",
            null,
            PromotionType.ProductDiscount,
            25m,
            null,
            null,
            null,
            null,
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow.AddDays(1),
            true,
            10,
            [productId],
            []));

        var response = await guestClient.PostAsJsonAsync("/api/orders", BuildOrderRequest(variantId, 1), JsonOptions);
        response.EnsureSuccessStatusCode();
        var order = await response.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        Assert.Equal(250m, order!.DiscountTotal);
        Assert.Single(order.AppliedPromotions);
        Assert.Equal("Haute priorité", order.AppliedPromotions[0].PromotionName);
    }
}
