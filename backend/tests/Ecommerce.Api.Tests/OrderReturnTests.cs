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

public class OrderReturnTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
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
            "Produit test retour",
            $"produit-retour-{unique}",
            null,
            2000m,
            [new CreateProductVariantRequest("Noir", "M", $"SKU-RET-{unique}", null, initialQuantity)]));
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDetailDto>();

        return (product!.Variants.Single().Id, adminClient);
    }

    private static CreateOrderRequest BuildOrderRequest(Guid variantId, int quantity) => new(
        "Amina",
        "Benali",
        "0551234567",
        "Alger",
        "Bab Ezzouar",
        "12 rue des Frères",
        DeliveryType.HomeDelivery,
        null,
        [new OrderItemRequest(variantId, quantity)]);

    private async Task<OrderDetailDto> WalkToStatusAsync(HttpClient adminClient, Guid orderId, params OrderStatus[] statuses)
    {
        OrderDetailDto? order = null;
        foreach (var status in statuses)
        {
            var response = await adminClient.PostAsJsonAsync($"/api/orders/{orderId}/status", new ChangeOrderStatusRequest(status, null), JsonOptions);
            response.EnsureSuccessStatusCode();
            order = await response.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);
        }

        return order!;
    }

    [Fact]
    public async Task DeliveredThenReturned_WrongSize_MovesStockFromSoldToReturned()
    {
        var (variantId, adminClient) = await CreateProductWithStockAsync(initialQuantity: 5);
        var guestClient = factory.CreateClient();

        var orderResponse = await guestClient.PostAsJsonAsync("/api/orders", BuildOrderRequest(variantId, 2), JsonOptions);
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        await WalkToStatusAsync(adminClient, order!.Id, OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.ReadyToShip, OrderStatus.Shipped, OrderStatus.OutForDelivery, OrderStatus.Delivered);

        var returnResponse = await adminClient.PostAsJsonAsync(
            $"/api/orders/{order.Id}/status",
            new ChangeOrderStatusRequest(OrderStatus.Returned, "Mauvaise taille", OrderReturnReason.WrongSize),
            JsonOptions);
        returnResponse.EnsureSuccessStatusCode();
        var returnedOrder = await returnResponse.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);
        Assert.Equal(OrderReturnReason.WrongSize, returnedOrder!.ReturnReason);

        var inventory = await (await adminClient.GetAsync($"/api/inventory/{variantId}")).Content.ReadFromJsonAsync<InventoryDto>();
        Assert.Equal(0, inventory!.SoldQuantity);
        Assert.Equal(2, inventory.ReturnedQuantity);
        Assert.Equal(0, inventory.DamagedQuantity);
    }

    [Fact]
    public async Task DeliveredThenReturned_Damaged_MovesStockFromSoldToDamaged()
    {
        var (variantId, adminClient) = await CreateProductWithStockAsync(initialQuantity: 5);
        var guestClient = factory.CreateClient();

        var orderResponse = await guestClient.PostAsJsonAsync("/api/orders", BuildOrderRequest(variantId, 2), JsonOptions);
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        await WalkToStatusAsync(adminClient, order!.Id, OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.ReadyToShip, OrderStatus.Shipped, OrderStatus.OutForDelivery, OrderStatus.Delivered);

        var returnResponse = await adminClient.PostAsJsonAsync(
            $"/api/orders/{order.Id}/status",
            new ChangeOrderStatusRequest(OrderStatus.Returned, "Colis endommagé", OrderReturnReason.Damaged),
            JsonOptions);
        returnResponse.EnsureSuccessStatusCode();

        var inventory = await (await adminClient.GetAsync($"/api/inventory/{variantId}")).Content.ReadFromJsonAsync<InventoryDto>();
        Assert.Equal(0, inventory!.SoldQuantity);
        Assert.Equal(0, inventory.ReturnedQuantity);
        Assert.Equal(2, inventory.DamagedQuantity);
    }

    [Fact]
    public async Task RefusedThenReturned_NotDamaged_DoesNotFail_AndStockStaysAvailable()
    {
        // Regression test: this exact path used to throw "Quantité vendue insuffisante pour
        // enregistrer ce retour." because Refused orders were never marked Sold in the first place.
        var (variantId, adminClient) = await CreateProductWithStockAsync(initialQuantity: 5);
        var guestClient = factory.CreateClient();

        var orderResponse = await guestClient.PostAsJsonAsync("/api/orders", BuildOrderRequest(variantId, 2), JsonOptions);
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        await WalkToStatusAsync(adminClient, order!.Id, OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.ReadyToShip, OrderStatus.Shipped, OrderStatus.OutForDelivery, OrderStatus.Refused);

        var inventoryAfterRefused = await (await adminClient.GetAsync($"/api/inventory/{variantId}")).Content.ReadFromJsonAsync<InventoryDto>();
        Assert.Equal(5, inventoryAfterRefused!.AvailableQuantity);

        var returnResponse = await adminClient.PostAsJsonAsync(
            $"/api/orders/{order.Id}/status",
            new ChangeOrderStatusRequest(OrderStatus.Returned, null, OrderReturnReason.CustomerChangedMind),
            JsonOptions);

        Assert.True(returnResponse.IsSuccessStatusCode, await returnResponse.Content.ReadAsStringAsync());

        var inventoryAfterReturn = await (await adminClient.GetAsync($"/api/inventory/{variantId}")).Content.ReadFromJsonAsync<InventoryDto>();
        Assert.Equal(5, inventoryAfterReturn!.AvailableQuantity);
        Assert.Equal(0, inventoryAfterReturn.DamagedQuantity);
    }

    [Fact]
    public async Task RefusedThenReturned_Damaged_MovesStockFromAvailableToDamaged()
    {
        var (variantId, adminClient) = await CreateProductWithStockAsync(initialQuantity: 5);
        var guestClient = factory.CreateClient();

        var orderResponse = await guestClient.PostAsJsonAsync("/api/orders", BuildOrderRequest(variantId, 2), JsonOptions);
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        await WalkToStatusAsync(adminClient, order!.Id, OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.ReadyToShip, OrderStatus.Shipped, OrderStatus.OutForDelivery, OrderStatus.Refused);

        var returnResponse = await adminClient.PostAsJsonAsync(
            $"/api/orders/{order.Id}/status",
            new ChangeOrderStatusRequest(OrderStatus.Returned, "Endommagé au retour", OrderReturnReason.Damaged),
            JsonOptions);
        returnResponse.EnsureSuccessStatusCode();

        var inventory = await (await adminClient.GetAsync($"/api/inventory/{variantId}")).Content.ReadFromJsonAsync<InventoryDto>();
        Assert.Equal(3, inventory!.AvailableQuantity);
        Assert.Equal(2, inventory.DamagedQuantity);
    }

    [Fact]
    public async Task DeliveryFailedThenReturned_NotDamaged_ReleasesReservedStockToAvailable()
    {
        // Regression test: DeliveryFailed never touched inventory, so the stock was still Reserved —
        // the old code tried to move it from Sold (0) and failed with the same insufficient-quantity error.
        var (variantId, adminClient) = await CreateProductWithStockAsync(initialQuantity: 5);
        var guestClient = factory.CreateClient();

        var orderResponse = await guestClient.PostAsJsonAsync("/api/orders", BuildOrderRequest(variantId, 2), JsonOptions);
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        await WalkToStatusAsync(adminClient, order!.Id, OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.ReadyToShip, OrderStatus.Shipped, OrderStatus.OutForDelivery, OrderStatus.DeliveryFailed);

        var inventoryAfterFailed = await (await adminClient.GetAsync($"/api/inventory/{variantId}")).Content.ReadFromJsonAsync<InventoryDto>();
        Assert.Equal(3, inventoryAfterFailed!.AvailableQuantity);
        Assert.Equal(2, inventoryAfterFailed.ReservedQuantity);

        var returnResponse = await adminClient.PostAsJsonAsync(
            $"/api/orders/{order.Id}/status",
            new ChangeOrderStatusRequest(OrderStatus.Returned, null, OrderReturnReason.Other),
            JsonOptions);

        Assert.True(returnResponse.IsSuccessStatusCode, await returnResponse.Content.ReadAsStringAsync());

        var inventoryAfterReturn = await (await adminClient.GetAsync($"/api/inventory/{variantId}")).Content.ReadFromJsonAsync<InventoryDto>();
        Assert.Equal(5, inventoryAfterReturn!.AvailableQuantity);
        Assert.Equal(0, inventoryAfterReturn.ReservedQuantity);
        Assert.Equal(0, inventoryAfterReturn.DamagedQuantity);
    }

    [Fact]
    public async Task DeliveryFailedThenReturned_Damaged_MovesReservedStockToDamaged()
    {
        var (variantId, adminClient) = await CreateProductWithStockAsync(initialQuantity: 5);
        var guestClient = factory.CreateClient();

        var orderResponse = await guestClient.PostAsJsonAsync("/api/orders", BuildOrderRequest(variantId, 2), JsonOptions);
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        await WalkToStatusAsync(adminClient, order!.Id, OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.ReadyToShip, OrderStatus.Shipped, OrderStatus.OutForDelivery, OrderStatus.DeliveryFailed);

        var returnResponse = await adminClient.PostAsJsonAsync(
            $"/api/orders/{order.Id}/status",
            new ChangeOrderStatusRequest(OrderStatus.Returned, "Colis abîmé", OrderReturnReason.Damaged),
            JsonOptions);
        returnResponse.EnsureSuccessStatusCode();

        var inventory = await (await adminClient.GetAsync($"/api/inventory/{variantId}")).Content.ReadFromJsonAsync<InventoryDto>();
        Assert.Equal(3, inventory!.AvailableQuantity);
        Assert.Equal(0, inventory.ReservedQuantity);
        Assert.Equal(2, inventory.DamagedQuantity);
    }

    [Fact]
    public async Task MarkReturned_WithoutReturnReason_ReturnsBadRequest()
    {
        var (variantId, adminClient) = await CreateProductWithStockAsync(initialQuantity: 5);
        var guestClient = factory.CreateClient();

        var orderResponse = await guestClient.PostAsJsonAsync("/api/orders", BuildOrderRequest(variantId, 1), JsonOptions);
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        await WalkToStatusAsync(adminClient, order!.Id, OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.ReadyToShip, OrderStatus.Shipped, OrderStatus.OutForDelivery, OrderStatus.Delivered);

        var response = await adminClient.PostAsJsonAsync(
            $"/api/orders/{order.Id}/status",
            new ChangeOrderStatusRequest(OrderStatus.Returned, null),
            JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetReturnReasonSummary_AggregatesCountsByReason()
    {
        var (variantId1, adminClient) = await CreateProductWithStockAsync(initialQuantity: 5);
        var (variantId2, _) = await CreateProductWithStockAsync(initialQuantity: 5);
        var guestClient = factory.CreateClient();

        async Task<Guid> CreateAndDeliverAsync(Guid variantId)
        {
            var orderResponse = await guestClient.PostAsJsonAsync("/api/orders", BuildOrderRequest(variantId, 1), JsonOptions);
            var order = await orderResponse.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);
            await WalkToStatusAsync(adminClient, order!.Id, OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.ReadyToShip, OrderStatus.Shipped, OrderStatus.OutForDelivery, OrderStatus.Delivered);
            return order.Id;
        }

        var orderId1 = await CreateAndDeliverAsync(variantId1);
        var orderId2 = await CreateAndDeliverAsync(variantId2);

        await adminClient.PostAsJsonAsync($"/api/orders/{orderId1}/status", new ChangeOrderStatusRequest(OrderStatus.Returned, null, OrderReturnReason.Damaged), JsonOptions);
        await adminClient.PostAsJsonAsync($"/api/orders/{orderId2}/status", new ChangeOrderStatusRequest(OrderStatus.Returned, null, OrderReturnReason.Damaged), JsonOptions);

        var summaryResponse = await adminClient.GetAsync("/api/orders/return-reasons");
        summaryResponse.EnsureSuccessStatusCode();
        var summary = await summaryResponse.Content.ReadFromJsonAsync<List<ReturnReasonSummaryDto>>(JsonOptions);

        var damagedEntry = summary!.Single(s => s.Reason == OrderReturnReason.Damaged);
        Assert.True(damagedEntry.Count >= 2);
    }
}
