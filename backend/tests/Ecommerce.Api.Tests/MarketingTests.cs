using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ecommerce.Application.Auth.Dtos;
using Ecommerce.Application.Catalog.Dtos;
using Ecommerce.Application.Marketing.Dtos;
using Ecommerce.Application.Orders.Dtos;
using Ecommerce.Domain.Orders;

namespace Ecommerce.Api.Tests;

public class MarketingTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
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

    private async Task<Guid> CreateVariantAsync(HttpClient adminClient)
    {
        var unique = Guid.NewGuid().ToString("N")[..8];

        var categoryResponse = await adminClient.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Cat {unique}", $"cat-{unique}", null, 0));
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>();

        var productResponse = await adminClient.PostAsJsonAsync("/api/products", new CreateProductRequest(
            category!.Id,
            "Produit test",
            $"produit-{unique}",
            null,
            2000m,
            [new CreateProductVariantRequest("Noir", "M", $"SKU-{unique}", null, 10)]));
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDetailDto>();

        return product!.Variants.Single().Id;
    }

    private static CreateOrderRequest BuildOrderRequest(Guid variantId, MarketingAttributionRequest? attribution, string phone) => new(
        "Amina",
        "Benali",
        phone,
        "Alger",
        "Bab Ezzouar",
        "12 rue des Frères",
        DeliveryType.HomeDelivery,
        null,
        [new OrderItemRequest(variantId, 1)],
        null,
        attribution);

    [Fact]
    public async Task CreateOrder_WithAttribution_StoresAndReturnsIt()
    {
        var adminClient = await CreateAuthenticatedClientAsync();
        var guestClient = factory.CreateClient();
        var variantId = await CreateVariantAsync(adminClient);

        var attribution = new MarketingAttributionRequest(
            "Facebook", "cpc", "Summer_2026", "robe_video_01", "robe",
            "fb.123", null, "https://facebook.com", "https://luna.local/product/robe");

        var response = await guestClient.PostAsJsonAsync("/api/orders", BuildOrderRequest(variantId, attribution, "0551234567"), JsonOptions);
        response.EnsureSuccessStatusCode();
        var order = await response.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        Assert.NotNull(order!.MarketingAttribution);
        Assert.Equal("Facebook", order.MarketingAttribution!.UtmSource);
        Assert.Equal("Summer_2026", order.MarketingAttribution.UtmCampaign);
        Assert.Equal("robe_video_01", order.MarketingAttribution.UtmContent);
        Assert.Equal("fb.123", order.MarketingAttribution.Fbclid);

        var fetched = await (await adminClient.GetAsync($"/api/orders/{order.Id}")).Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);
        Assert.Equal("Facebook", fetched!.MarketingAttribution!.UtmSource);
    }

    [Fact]
    public async Task CreateOrder_WithoutAttribution_HasNullMarketingAttribution()
    {
        var adminClient = await CreateAuthenticatedClientAsync();
        var guestClient = factory.CreateClient();
        var variantId = await CreateVariantAsync(adminClient);

        var response = await guestClient.PostAsJsonAsync("/api/orders", BuildOrderRequest(variantId, null, "0559876543"), JsonOptions);
        response.EnsureSuccessStatusCode();
        var order = await response.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        Assert.Null(order!.MarketingAttribution);
    }

    [Fact]
    public async Task MarketingSources_AggregatesByUtmSource_WithDirectBucketForNoSource()
    {
        var adminClient = await CreateAuthenticatedClientAsync();
        var guestClient = factory.CreateClient();
        var variantId = await CreateVariantAsync(adminClient);

        var sourceName = $"TestSource-{Guid.NewGuid():N}"[..20];
        var attribution = new MarketingAttributionRequest(sourceName, "cpc", "Camp", null, null, null, null, null, null);

        var withSource1 = await guestClient.PostAsJsonAsync("/api/orders", BuildOrderRequest(variantId, attribution, "0551111111"), JsonOptions);
        withSource1.EnsureSuccessStatusCode();
        var withSource2 = await guestClient.PostAsJsonAsync("/api/orders", BuildOrderRequest(variantId, attribution, "0552222222"), JsonOptions);
        withSource2.EnsureSuccessStatusCode();
        var direct = await guestClient.PostAsJsonAsync("/api/orders", BuildOrderRequest(variantId, null, "0553333333"), JsonOptions);
        direct.EnsureSuccessStatusCode();

        var summaryResponse = await adminClient.GetAsync("/api/marketing/sources?days=1");
        summaryResponse.EnsureSuccessStatusCode();
        var summary = await summaryResponse.Content.ReadFromJsonAsync<List<MarketingSourceSummaryDto>>(JsonOptions) ?? [];

        var sourceEntry = summary.Single(s => s.Source == sourceName);
        Assert.Equal(2, sourceEntry.OrderCount);
        Assert.True(sourceEntry.TotalRevenue > 0);

        Assert.Contains(summary, s => s.Source == "Direct");
    }
}
