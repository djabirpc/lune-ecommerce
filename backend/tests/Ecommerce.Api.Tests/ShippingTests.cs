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
using Ecommerce.Domain.Shipping;

namespace Ecommerce.Api.Tests;

public class ShippingTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
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

    private async Task<OrderDetailDto> CreateOrderReadyToShipAsync(HttpClient adminClient, HttpClient guestClient)
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
        var variantId = product!.Variants.Single().Id;

        var orderRequest = new CreateOrderRequest(
            "Amina",
            "Benali",
            "0551234567",
            "Alger",
            "Bab Ezzouar",
            "12 rue des Frères",
            DeliveryType.HomeDelivery,
            null,
            [new OrderItemRequest(variantId, 1)]);

        var orderResponse = await guestClient.PostAsJsonAsync("/api/orders", orderRequest, JsonOptions);
        orderResponse.EnsureSuccessStatusCode();
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        foreach (var status in new[] { OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.ReadyToShip })
        {
            var response = await adminClient.PostAsJsonAsync($"/api/orders/{order!.Id}/status", new ChangeOrderStatusRequest(status, null), JsonOptions);
            response.EnsureSuccessStatusCode();
            order = await response.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);
        }

        return order!;
    }

    [Fact]
    public async Task CreateShipment_TransitionsOrderToShipped_AndCreatesShipment()
    {
        var adminClient = await CreateAuthenticatedClientAsync();
        var guestClient = factory.CreateClient();
        var order = await CreateOrderReadyToShipAsync(adminClient, guestClient);

        var response = await adminClient.PostAsJsonAsync(
            $"/api/orders/{order.Id}/shipment",
            new CreateShipmentRequest(ShippingCarrier.Fake),
            JsonOptions);
        response.EnsureSuccessStatusCode();
        var shipment = await response.Content.ReadFromJsonAsync<ShipmentDto>(JsonOptions);

        Assert.Equal(ShippingCarrier.Fake, shipment!.Carrier);
        Assert.NotNull(shipment.TrackingNumber);
        Assert.Equal(NormalizedShippingStatus.Created, shipment.NormalizedStatus);
        Assert.Single(shipment.TrackingEvents);

        var updatedOrder = await (await adminClient.GetAsync($"/api/orders/{order.Id}")).Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);
        Assert.Equal(OrderStatus.Shipped, updatedOrder!.Status);
        Assert.NotNull(updatedOrder.Shipment);
        Assert.Equal(shipment.Id, updatedOrder.Shipment!.Id);
    }

    [Fact]
    public async Task SyncTracking_ProgressesStatus_AndEventuallyDeliversOrder()
    {
        var adminClient = await CreateAuthenticatedClientAsync();
        var guestClient = factory.CreateClient();
        var order = await CreateOrderReadyToShipAsync(adminClient, guestClient);

        var createResponse = await adminClient.PostAsJsonAsync(
            $"/api/orders/{order.Id}/shipment",
            new CreateShipmentRequest(ShippingCarrier.Fake),
            JsonOptions);
        var shipment = await createResponse.Content.ReadFromJsonAsync<ShipmentDto>(JsonOptions);

        // Fake sequence: Created -> PickedUp -> InTransit -> AtDestination -> OutForDelivery -> Delivered
        ShipmentDto? synced = null;
        for (var i = 0; i < 5; i++)
        {
            var syncResponse = await adminClient.PostAsync($"/api/shipments/{shipment!.Id}/sync", null);
            syncResponse.EnsureSuccessStatusCode();
            synced = await syncResponse.Content.ReadFromJsonAsync<ShipmentDto>(JsonOptions);
        }

        Assert.Equal(NormalizedShippingStatus.Delivered, synced!.NormalizedStatus);
        Assert.Equal(6, synced.TrackingEvents.Count);

        var updatedOrder = await (await adminClient.GetAsync($"/api/orders/{order.Id}")).Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);
        Assert.Equal(OrderStatus.Delivered, updatedOrder!.Status);
        Assert.Equal(Domain.Orders.PaymentStatus.Collected, updatedOrder.PaymentStatus);
    }

    [Fact]
    public async Task CreateShipment_ForOrderNotReadyToShip_ReturnsConflict()
    {
        var adminClient = await CreateAuthenticatedClientAsync();
        var guestClient = factory.CreateClient();

        var unique = Guid.NewGuid().ToString("N")[..8];
        var categoryResponse = await adminClient.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Cat {unique}", $"cat-{unique}", null, 0));
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>();
        var productResponse = await adminClient.PostAsJsonAsync("/api/products", new CreateProductRequest(
            category!.Id, "Produit test", $"produit-{unique}", null, 2000m,
            [new CreateProductVariantRequest("Noir", "M", $"SKU-{unique}", null, 10)]));
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDetailDto>();

        var orderRequest = new CreateOrderRequest(
            "Amina", "Benali", "0551234567", "Alger", "Bab Ezzouar", "12 rue des Frères",
            DeliveryType.HomeDelivery, null, [new OrderItemRequest(product!.Variants.Single().Id, 1)]);
        var orderResponse = await guestClient.PostAsJsonAsync("/api/orders", orderRequest, JsonOptions);
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        var response = await adminClient.PostAsJsonAsync(
            $"/api/orders/{order!.Id}/shipment",
            new CreateShipmentRequest(ShippingCarrier.Fake),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateShipment_Twice_ReturnsConflict()
    {
        var adminClient = await CreateAuthenticatedClientAsync();
        var guestClient = factory.CreateClient();
        var order = await CreateOrderReadyToShipAsync(adminClient, guestClient);

        var first = await adminClient.PostAsJsonAsync($"/api/orders/{order.Id}/shipment", new CreateShipmentRequest(ShippingCarrier.Fake), JsonOptions);
        first.EnsureSuccessStatusCode();

        var second = await adminClient.PostAsJsonAsync($"/api/orders/{order.Id}/shipment", new CreateShipmentRequest(ShippingCarrier.Fake), JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task CreateShipment_WithYalidine_ReturnsNotConfigured()
    {
        var adminClient = await CreateAuthenticatedClientAsync();
        var guestClient = factory.CreateClient();
        var order = await CreateOrderReadyToShipAsync(adminClient, guestClient);

        var response = await adminClient.PostAsJsonAsync(
            $"/api/orders/{order.Id}/shipment",
            new CreateShipmentRequest(ShippingCarrier.Yalidine),
            JsonOptions);

        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
    }

    [Fact]
    public async Task GetCarriers_ReturnsFakeAvailable_OthersNotConfigured()
    {
        var adminClient = await CreateAuthenticatedClientAsync();

        var response = await adminClient.GetAsync("/api/shipping/carriers");
        response.EnsureSuccessStatusCode();
        var carriers = await response.Content.ReadFromJsonAsync<List<ShippingCarrierAvailabilityDto>>(JsonOptions) ?? [];

        Assert.True(carriers.Single(c => c.Carrier == ShippingCarrier.Fake).IsConfigured);
        Assert.False(carriers.Single(c => c.Carrier == ShippingCarrier.Yalidine).IsConfigured);
        Assert.False(carriers.Single(c => c.Carrier == ShippingCarrier.ZRExpress).IsConfigured);
    }
}
