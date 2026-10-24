using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ecommerce.Application.Auth.Dtos;
using Ecommerce.Application.Catalog.Dtos;
using Ecommerce.Application.Marketing.Dtos;
using Ecommerce.Application.Orders.Dtos;
using Ecommerce.Domain.Marketing;
using Ecommerce.Domain.Orders;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.Api.Tests;

public class MarketingSettingsTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
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

    [Fact]
    public async Task GetSettings_WithoutAuth_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/marketing/settings");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPurchaseTrigger_WithoutAuth_IsPublicAndDefaultsToOrderCreated()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/marketing/purchase-trigger");
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<PurchaseTriggerDto>(JsonOptions);

        Assert.Equal(PurchaseEventTrigger.OrderCreated, dto!.Trigger);
    }

    [Fact]
    public async Task UpdateSettings_SetsTriggerAndCredentials_NeverEchoesTokenBack()
    {
        var adminClient = await CreateAuthenticatedClientAsync();
        const string secretToken = "super-secret-meta-token-value";

        var updateResponse = await adminClient.PutAsJsonAsync("/api/marketing/settings", new UpdateMarketingSettingsRequest(
            PurchaseEventTrigger.OrderConfirmed, "1234567890123456", secretToken, false, "TTPIXEL", "tiktok-secret", false), JsonOptions);
        updateResponse.EnsureSuccessStatusCode();

        var rawBody = await updateResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain(secretToken, rawBody);
        Assert.DoesNotContain("tiktok-secret", rawBody);

        var updated = await updateResponse.Content.ReadFromJsonAsync<MarketingSettingsDto>(JsonOptions);
        Assert.Equal(PurchaseEventTrigger.OrderConfirmed, updated!.PurchaseEventTrigger);
        Assert.Equal("1234567890123456", updated.MetaPixelId);
        Assert.True(updated.IsMetaAccessTokenConfigured);
        Assert.Equal("TTPIXEL", updated.TikTokPixelCode);
        Assert.True(updated.IsTikTokAccessTokenConfigured);

        var getResponse = await adminClient.GetAsync("/api/marketing/settings");
        var fetched = await getResponse.Content.ReadFromJsonAsync<MarketingSettingsDto>(JsonOptions);
        Assert.Equal(PurchaseEventTrigger.OrderConfirmed, fetched!.PurchaseEventTrigger);
        Assert.True(fetched.IsMetaAccessTokenConfigured);

        // The public endpoint must reflect the new trigger immediately.
        var publicTrigger = await (await factory.CreateClient().GetAsync("/api/marketing/purchase-trigger"))
            .Content.ReadFromJsonAsync<PurchaseTriggerDto>(JsonOptions);
        Assert.Equal(PurchaseEventTrigger.OrderConfirmed, publicTrigger!.Trigger);

        // Reset back to the default so other tests in this shared-DB fixture aren't affected.
        await adminClient.PutAsJsonAsync("/api/marketing/settings", new UpdateMarketingSettingsRequest(
            PurchaseEventTrigger.OrderCreated, null, null, true, null, null, true), JsonOptions);
    }

    [Fact]
    public async Task UpdateSettings_ClearAccessToken_RemovesIt()
    {
        var adminClient = await CreateAuthenticatedClientAsync();

        await adminClient.PutAsJsonAsync("/api/marketing/settings", new UpdateMarketingSettingsRequest(
            PurchaseEventTrigger.OrderCreated, "9876543210", "a-token", false, null, null, false), JsonOptions);

        var clearResponse = await adminClient.PutAsJsonAsync("/api/marketing/settings", new UpdateMarketingSettingsRequest(
            PurchaseEventTrigger.OrderCreated, "9876543210", null, true, null, null, false), JsonOptions);
        clearResponse.EnsureSuccessStatusCode();
        var cleared = await clearResponse.Content.ReadFromJsonAsync<MarketingSettingsDto>(JsonOptions);

        Assert.False(cleared!.IsMetaAccessTokenConfigured);
        Assert.Equal("9876543210", cleared.MetaPixelId);
    }

    [Fact]
    public async Task UpdateSettings_OmittingToken_KeepsThePreviouslySetOne()
    {
        var adminClient = await CreateAuthenticatedClientAsync();

        await adminClient.PutAsJsonAsync("/api/marketing/settings", new UpdateMarketingSettingsRequest(
            PurchaseEventTrigger.OrderCreated, "1111111111", "kept-token", false, null, null, false), JsonOptions);

        // Re-save with a different Pixel ID but no token in the request — the existing token must survive.
        var response = await adminClient.PutAsJsonAsync("/api/marketing/settings", new UpdateMarketingSettingsRequest(
            PurchaseEventTrigger.OrderCreated, "2222222222", null, false, null, null, false), JsonOptions);
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<MarketingSettingsDto>(JsonOptions);

        Assert.Equal("2222222222", dto!.MetaPixelId);
        Assert.True(dto.IsMetaAccessTokenConfigured);

        // Cleanup.
        await adminClient.PutAsJsonAsync("/api/marketing/settings", new UpdateMarketingSettingsRequest(
            PurchaseEventTrigger.OrderCreated, null, null, true, null, null, true), JsonOptions);
    }

    private async Task<Guid> CreateVariantAsync(HttpClient adminClient)
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        var categoryResponse = await adminClient.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Cat {unique}", $"cat-{unique}", null, 0));
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>();
        var productResponse = await adminClient.PostAsJsonAsync("/api/products", new CreateProductRequest(
            category!.Id, "Produit test", $"produit-{unique}", null, 2000m,
            [new CreateProductVariantRequest("Noir", "M", $"SKU-{unique}", null, 10)]));
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDetailDto>();
        return product!.Variants.Single().Id;
    }

    private async Task<Guid> PlaceOrderAsync(HttpClient adminClient, Guid variantId, string phone)
    {
        var guestClient = factory.CreateClient();
        var response = await guestClient.PostAsJsonAsync("/api/orders", new CreateOrderRequest(
            "Amina", "Benali", phone, "Alger", "Bab Ezzouar", "12 rue des Frères",
            DeliveryType.HomeDelivery, null, [new OrderItemRequest(variantId, 1)]), JsonOptions);
        response.EnsureSuccessStatusCode();
        var order = await response.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);
        return order!.Id;
    }

    private async Task<DateTime?> GetPurchaseEventSentAtAsync(Guid orderId)
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await dbContext.Orders.AsNoTracking().Where(o => o.Id == orderId).Select(o => o.PurchaseEventSentAtUtc).SingleAsync();
    }

    [Fact]
    public async Task ConfirmingOrder_UnderOrderCreatedTrigger_NeverStampsPurchaseEventSentAt()
    {
        var adminClient = await CreateAuthenticatedClientAsync();
        var variantId = await CreateVariantAsync(adminClient);
        var orderId = await PlaceOrderAsync(adminClient, variantId, "0560000001");

        var response = await adminClient.PostAsJsonAsync($"/api/orders/{orderId}/status", new ChangeOrderStatusRequest(OrderStatus.Confirmed, null), JsonOptions);
        response.EnsureSuccessStatusCode();

        Assert.Null(await GetPurchaseEventSentAtAsync(orderId));
    }

    [Fact]
    public async Task ConfirmingOrder_UnderOrderConfirmedTrigger_StampsPurchaseEventSentAt()
    {
        var adminClient = await CreateAuthenticatedClientAsync();
        await adminClient.PutAsJsonAsync("/api/marketing/settings", new UpdateMarketingSettingsRequest(
            PurchaseEventTrigger.OrderConfirmed, null, null, false, null, null, false), JsonOptions);

        try
        {
            var variantId = await CreateVariantAsync(adminClient);
            var orderId = await PlaceOrderAsync(adminClient, variantId, "0560000002");

            var response = await adminClient.PostAsJsonAsync($"/api/orders/{orderId}/status", new ChangeOrderStatusRequest(OrderStatus.Confirmed, null), JsonOptions);
            response.EnsureSuccessStatusCode();

            // Neither Meta nor TikTok is configured, so no real HTTP call happens — but the trigger
            // point itself still gets marked as reached (idempotency bookkeeping, see
            // OrderService.MaybeSendPurchaseConversionEventAsync).
            Assert.NotNull(await GetPurchaseEventSentAtAsync(orderId));
        }
        finally
        {
            await adminClient.PutAsJsonAsync("/api/marketing/settings", new UpdateMarketingSettingsRequest(
                PurchaseEventTrigger.OrderCreated, null, null, true, null, null, true), JsonOptions);
        }
    }

    [Fact]
    public async Task CreatingAdminOrder_UnderOrderConfirmedTrigger_StampsPurchaseEventSentAtImmediately()
    {
        var adminClient = await CreateAuthenticatedClientAsync();
        await adminClient.PutAsJsonAsync("/api/marketing/settings", new UpdateMarketingSettingsRequest(
            PurchaseEventTrigger.OrderConfirmed, null, null, false, null, null, false), JsonOptions);

        try
        {
            var variantId = await CreateVariantAsync(adminClient);
            var response = await adminClient.PostAsJsonAsync("/api/orders/admin", new CreateAdminOrderRequest(
                "Amina", "Benali", "0560000003", "Alger", "Bab Ezzouar", "12 rue des Frères",
                DeliveryType.HomeDelivery, null, [new OrderItemRequest(variantId, 1)], null, null, false), JsonOptions);
            response.EnsureSuccessStatusCode();
            var order = await response.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

            Assert.Equal(OrderStatus.Confirmed, order!.Status);
            Assert.NotNull(await GetPurchaseEventSentAtAsync(order.Id));
        }
        finally
        {
            await adminClient.PutAsJsonAsync("/api/marketing/settings", new UpdateMarketingSettingsRequest(
                PurchaseEventTrigger.OrderCreated, null, null, true, null, null, true), JsonOptions);
        }
    }
}
