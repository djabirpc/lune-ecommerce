using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ecommerce.Application.Auth.Dtos;
using Ecommerce.Application.Catalog.Dtos;
using Ecommerce.Application.Orders.Dtos;
using Ecommerce.Application.Shipping.Dtos;
using Ecommerce.Domain.Orders;

namespace Ecommerce.Api.Tests;

public class ShippingRateTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
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

    private async Task<Guid> CreateVariantAsync(HttpClient adminClient, decimal price = 1000m)
    {
        var unique = Guid.NewGuid().ToString("N")[..8];

        var categoryResponse = await adminClient.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Cat {unique}", $"cat-{unique}", null, 0));
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>();

        var productResponse = await adminClient.PostAsJsonAsync("/api/products", new CreateProductRequest(
            category!.Id,
            "Produit test",
            $"produit-{unique}",
            null,
            price,
            [new CreateProductVariantRequest("Noir", "M", $"SKU-{unique}", null, 10)]));
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDetailDto>();

        return product!.Variants.Single().Id;
    }

    private static CreateOrderRequest BuildOrderRequest(Guid variantId, string wilaya, DeliveryType deliveryType) => new(
        "Amina", "Benali", "0551234567", wilaya, "Centre-ville", "12 rue des Frères",
        deliveryType, null, [new OrderItemRequest(variantId, 1)]);

    [Fact]
    public async Task Quote_ReturnsSeededDefaultRate_ForHomeDeliveryAndStopDesk()
    {
        var client = factory.CreateClient();

        var homeResponse = await client.GetAsync("/api/shipping-rates/quote?wilaya=Alger&deliveryType=HomeDelivery");
        homeResponse.EnsureSuccessStatusCode();
        var homeQuote = await homeResponse.Content.ReadFromJsonAsync<ShippingQuoteDto>(JsonOptions);

        var stopDeskResponse = await client.GetAsync("/api/shipping-rates/quote?wilaya=Alger&deliveryType=StopDesk");
        stopDeskResponse.EnsureSuccessStatusCode();
        var stopDeskQuote = await stopDeskResponse.Content.ReadFromJsonAsync<ShippingQuoteDto>(JsonOptions);

        Assert.Equal(600m, homeQuote!.Price);
        Assert.Equal(400m, stopDeskQuote!.Price);
    }

    [Fact]
    public async Task Quote_ForUnknownWilaya_ReturnsBadRequest()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/shipping-rates/quote?wilaya=Atlantide&deliveryType=HomeDelivery");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_UsesConfiguredRate_ForWilayaAndDeliveryType()
    {
        var adminClient = await CreateAuthenticatedClientAsync();
        var guestClient = factory.CreateClient();
        var variantId = await CreateVariantAsync(adminClient, price: 1000m);

        var homeResponse = await guestClient.PostAsJsonAsync("/api/orders", BuildOrderRequest(variantId, "Oran", DeliveryType.HomeDelivery), JsonOptions);
        homeResponse.EnsureSuccessStatusCode();
        var homeOrder = await homeResponse.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        var stopDeskResponse = await guestClient.PostAsJsonAsync("/api/orders", BuildOrderRequest(variantId, "Oran", DeliveryType.StopDesk), JsonOptions);
        stopDeskResponse.EnsureSuccessStatusCode();
        var stopDeskOrder = await stopDeskResponse.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        Assert.Equal(600m, homeOrder!.ShippingCost);
        Assert.Equal(1600m, homeOrder.Total);
        Assert.Equal(400m, stopDeskOrder!.ShippingCost);
        Assert.Equal(1400m, stopDeskOrder.Total);
    }

    [Fact]
    public async Task CreateOrder_ForUnknownWilaya_ReturnsBadRequest()
    {
        var adminClient = await CreateAuthenticatedClientAsync();
        var guestClient = factory.CreateClient();
        var variantId = await CreateVariantAsync(adminClient);

        var response = await guestClient.PostAsJsonAsync("/api/orders", BuildOrderRequest(variantId, "Atlantide", DeliveryType.HomeDelivery), JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AdminUpdatesRate_SubsequentOrderUsesNewPrice()
    {
        var adminClient = await CreateAuthenticatedClientAsync();
        var guestClient = factory.CreateClient();
        var variantId = await CreateVariantAsync(adminClient, price: 1000m);

        var updateResponse = await adminClient.PutAsJsonAsync(
            "/api/shipping-rates/Tamanrasset",
            new UpdateShippingRateRequest(1200m, 900m, true),
            JsonOptions);
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<ShippingRateDto>(JsonOptions);
        Assert.Equal(1200m, updated!.HomeDeliveryPrice);

        var orderResponse = await guestClient.PostAsJsonAsync("/api/orders", BuildOrderRequest(variantId, "Tamanrasset", DeliveryType.HomeDelivery), JsonOptions);
        orderResponse.EnsureSuccessStatusCode();
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        Assert.Equal(1200m, order!.ShippingCost);
    }

    [Fact]
    public async Task AdminDeactivatesRate_OrdersForThatWilayaAreRejected()
    {
        var adminClient = await CreateAuthenticatedClientAsync();
        var guestClient = factory.CreateClient();
        var variantId = await CreateVariantAsync(adminClient);

        var deactivateResponse = await adminClient.PutAsJsonAsync(
            "/api/shipping-rates/Illizi",
            new UpdateShippingRateRequest(600m, 400m, false),
            JsonOptions);
        deactivateResponse.EnsureSuccessStatusCode();

        var orderResponse = await guestClient.PostAsJsonAsync("/api/orders", BuildOrderRequest(variantId, "Illizi", DeliveryType.HomeDelivery), JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, orderResponse.StatusCode);

        // Restore for any other test that might use this wilaya.
        await adminClient.PutAsJsonAsync("/api/shipping-rates/Illizi", new UpdateShippingRateRequest(600m, 400m, true), JsonOptions);
    }

    [Fact]
    public async Task GetAllRates_WithoutAuth_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/shipping-rates");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAllRates_AsAdmin_Returns58SeededWilayas()
    {
        var adminClient = await CreateAuthenticatedClientAsync();

        var response = await adminClient.GetAsync("/api/shipping-rates");
        response.EnsureSuccessStatusCode();
        var rates = await response.Content.ReadFromJsonAsync<List<ShippingRateDto>>(JsonOptions) ?? [];

        Assert.Equal(58, rates.Count);
    }
}
