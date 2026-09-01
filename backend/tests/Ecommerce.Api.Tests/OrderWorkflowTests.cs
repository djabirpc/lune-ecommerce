using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ecommerce.Application.Auth.Dtos;
using Ecommerce.Application.Catalog.Dtos;
using Ecommerce.Application.Inventory.Dtos;
using Ecommerce.Application.Orders.Dtos;
using Ecommerce.Domain.Orders;

namespace Ecommerce.Api.Tests;

public class OrderWorkflowTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
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

    private async Task<(Guid VariantId, HttpClient AdminClient)> CreateProductWithStockAsync(int initialQuantity)
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
            2000m,
            [new CreateProductVariantRequest("Noir", "M", $"SKU-{unique}", null, initialQuantity)]));
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDetailDto>();

        return (product!.Variants.Single().Id, adminClient);
    }

    private static CreateOrderRequest BuildOrderRequest(Guid variantId, int quantity, string phone = "0551234567") => new(
        "Amina",
        "Benali",
        phone,
        "Alger",
        "Bab Ezzouar",
        "12 rue des Frères",
        DeliveryType.HomeDelivery,
        null,
        [new OrderItemRequest(variantId, quantity)]);

    [Fact]
    public async Task CreateOrder_ReserveStock_Cancel_ReleaseStock()
    {
        var (variantId, adminClient) = await CreateProductWithStockAsync(initialQuantity: 10);
        var guestClient = factory.CreateClient();

        var orderResponse = await guestClient.PostAsJsonAsync("/api/orders", BuildOrderRequest(variantId, 3), JsonOptions);
        orderResponse.EnsureSuccessStatusCode();
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);
        Assert.Equal(OrderStatus.PendingConfirmation, order!.Status);

        var inventoryAfterOrder = await (await adminClient.GetAsync($"/api/inventory/{variantId}")).Content.ReadFromJsonAsync<InventoryDto>();
        Assert.Equal(7, inventoryAfterOrder!.AvailableQuantity);
        Assert.Equal(3, inventoryAfterOrder.ReservedQuantity);

        var cancelResponse = await adminClient.PostAsJsonAsync($"/api/orders/{order.Id}/status", new ChangeOrderStatusRequest(OrderStatus.Cancelled, "Client a annulé"), JsonOptions);
        cancelResponse.EnsureSuccessStatusCode();
        var cancelledOrder = await cancelResponse.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);
        Assert.Equal(OrderStatus.Cancelled, cancelledOrder!.Status);

        var inventoryAfterCancel = await (await adminClient.GetAsync($"/api/inventory/{variantId}")).Content.ReadFromJsonAsync<InventoryDto>();
        Assert.Equal(10, inventoryAfterCancel!.AvailableQuantity);
        Assert.Equal(0, inventoryAfterCancel.ReservedQuantity);
    }

    [Fact]
    public async Task CreateOrder_ReserveStock_ConfirmPrepareShipDeliver_RecordsSale()
    {
        var (variantId, adminClient) = await CreateProductWithStockAsync(initialQuantity: 5);
        var guestClient = factory.CreateClient();

        var orderResponse = await guestClient.PostAsJsonAsync("/api/orders", BuildOrderRequest(variantId, 2), JsonOptions);
        orderResponse.EnsureSuccessStatusCode();
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        foreach (var status in new[]
                 {
                     OrderStatus.Confirmed,
                     OrderStatus.Preparing,
                     OrderStatus.ReadyToShip,
                     OrderStatus.Shipped,
                     OrderStatus.OutForDelivery,
                     OrderStatus.Delivered,
                 })
        {
            var response = await adminClient.PostAsJsonAsync($"/api/orders/{order!.Id}/status", new ChangeOrderStatusRequest(status, null), JsonOptions);
            response.EnsureSuccessStatusCode();
            order = await response.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);
        }

        Assert.Equal(OrderStatus.Delivered, order!.Status);
        Assert.Equal(Domain.Orders.PaymentStatus.Collected, order.PaymentStatus);

        var inventory = await (await adminClient.GetAsync($"/api/inventory/{variantId}")).Content.ReadFromJsonAsync<InventoryDto>();
        Assert.Equal(3, inventory!.AvailableQuantity);
        Assert.Equal(0, inventory.ReservedQuantity);
        Assert.Equal(2, inventory.SoldQuantity);
    }

    [Fact]
    public async Task ChangeStatus_InvalidTransition_ReturnsConflict()
    {
        var (variantId, adminClient) = await CreateProductWithStockAsync(initialQuantity: 5);
        var guestClient = factory.CreateClient();

        var orderResponse = await guestClient.PostAsJsonAsync("/api/orders", BuildOrderRequest(variantId, 1), JsonOptions);
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        var response = await adminClient.PostAsJsonAsync($"/api/orders/{order!.Id}/status", new ChangeOrderStatusRequest(OrderStatus.Delivered, null), JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateOrder_InsufficientStockOnOneItem_RollsBackWholeOrderAndDoesNotTouchOtherItemStock()
    {
        var (scarceVariantId, adminClient) = await CreateProductWithStockAsync(initialQuantity: 1);
        var (plentifulVariantId, _) = await CreateProductWithStockAsync(initialQuantity: 20);
        var guestClient = factory.CreateClient();

        var request = new CreateOrderRequest(
            "Amina",
            "Benali",
            "0551234567",
            "Alger",
            "Bab Ezzouar",
            "12 rue des Frères",
            DeliveryType.HomeDelivery,
            null,
            [new OrderItemRequest(scarceVariantId, 5), new OrderItemRequest(plentifulVariantId, 2)]);

        var response = await guestClient.PostAsJsonAsync("/api/orders", request, JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        var plentifulInventory = await (await adminClient.GetAsync($"/api/inventory/{plentifulVariantId}")).Content.ReadFromJsonAsync<InventoryDto>();
        Assert.Equal(20, plentifulInventory!.AvailableQuantity);
        Assert.Equal(0, plentifulInventory.ReservedQuantity);
    }

    [Fact]
    public async Task Track_WithWrongPhone_ReturnsNotFound_WithCorrectPhone_ReturnsOrder()
    {
        var (variantId, _) = await CreateProductWithStockAsync(initialQuantity: 5);
        var guestClient = factory.CreateClient();

        var orderResponse = await guestClient.PostAsJsonAsync("/api/orders", BuildOrderRequest(variantId, 1, "0559876543"), JsonOptions);
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        var wrongPhoneResponse = await guestClient.GetAsync($"/api/orders/track?orderNumber={order!.OrderNumber}&phone=0500000000");
        Assert.Equal(HttpStatusCode.NotFound, wrongPhoneResponse.StatusCode);

        var correctPhoneResponse = await guestClient.GetAsync($"/api/orders/track?orderNumber={order.OrderNumber}&phone=0559876543");
        correctPhoneResponse.EnsureSuccessStatusCode();
    }
}
