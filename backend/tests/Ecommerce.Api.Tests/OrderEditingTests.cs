using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ecommerce.Application.Auth.Dtos;
using Ecommerce.Application.Catalog.Dtos;
using Ecommerce.Application.Inventory.Dtos;
using Ecommerce.Application.Orders.Dtos;
using Ecommerce.Application.Promotions.Dtos;
using Ecommerce.Domain.Orders;
using Ecommerce.Domain.Promotions;

namespace Ecommerce.Api.Tests;

public class OrderEditingTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
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

    private async Task<(Guid VariantId, HttpClient AdminClient)> CreateProductWithStockAsync(int initialQuantity, decimal price = 1000m)
    {
        var adminClient = await CreateAuthenticatedClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];

        var categoryResponse = await adminClient.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Cat {unique}", $"cat-{unique}", null, 0));
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>();

        var productResponse = await adminClient.PostAsJsonAsync("/api/products", new CreateProductRequest(
            category!.Id, "Produit test", $"produit-{unique}", null, price,
            [new CreateProductVariantRequest("Noir", "M", $"SKU-{unique}", null, initialQuantity)]));
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDetailDto>();

        return (product!.Variants.Single().Id, adminClient);
    }

    private async Task<OrderDetailDto> CreateOrderAsync(HttpClient adminClient, Guid variantId, int quantity)
    {
        var guestClient = factory.CreateClient();
        var request = new CreateOrderRequest(
            "Amina", "Benali", "0551234567", "Alger", "Bab Ezzouar", "12 rue des Frères",
            DeliveryType.HomeDelivery, null, [new OrderItemRequest(variantId, quantity)]);

        var response = await guestClient.PostAsJsonAsync("/api/orders", request, JsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions))!;
    }

    [Fact]
    public async Task AddItem_ToNewVariant_AddsLineAndReservesStock()
    {
        var (variantId, adminClient) = await CreateProductWithStockAsync(10);
        var order = await CreateOrderAsync(adminClient, variantId, 1);

        var (secondVariantId, _) = await CreateProductWithStockAsync(10, price: 500m);

        var response = await adminClient.PostAsJsonAsync(
            $"/api/orders/{order.Id}/items", new AddOrderItemRequest(secondVariantId, 2), JsonOptions);
        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        Assert.Equal(2, updated!.Items.Count);
        Assert.Equal(2000m, updated.Subtotal); // 1000 (first item) + 500*2 (second item)

        var inventory = await (await adminClient.GetAsync($"/api/inventory/{secondVariantId}")).Content.ReadFromJsonAsync<InventoryDto>();
        Assert.Equal(2, inventory!.ReservedQuantity);
    }

    [Fact]
    public async Task AddItem_SameVariantAgain_MergesIntoExistingLine()
    {
        var (variantId, adminClient) = await CreateProductWithStockAsync(10);
        var order = await CreateOrderAsync(adminClient, variantId, 1);

        var response = await adminClient.PostAsJsonAsync(
            $"/api/orders/{order.Id}/items", new AddOrderItemRequest(variantId, 2), JsonOptions);
        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        Assert.Single(updated!.Items);
        Assert.Equal(3, updated.Items[0].Quantity);
        Assert.Equal(3000m, updated.Subtotal);
    }

    [Fact]
    public async Task AddItem_ToShippedOrder_ReturnsConflict()
    {
        var (variantId, adminClient) = await CreateProductWithStockAsync(10);
        var order = await CreateOrderAsync(adminClient, variantId, 1);

        foreach (var status in new[] { OrderStatus.Confirmed, OrderStatus.Preparing, OrderStatus.ReadyToShip, OrderStatus.Shipped })
        {
            var statusResponse = await adminClient.PostAsJsonAsync(
                $"/api/orders/{order.Id}/status", new ChangeOrderStatusRequest(status, null), JsonOptions);
            statusResponse.EnsureSuccessStatusCode();
        }

        var (otherVariantId, _) = await CreateProductWithStockAsync(10);
        var response = await adminClient.PostAsJsonAsync(
            $"/api/orders/{order.Id}/items", new AddOrderItemRequest(otherVariantId, 1), JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task RemoveItem_ReleasesStockAndRecalculatesTotal()
    {
        var (variantId, adminClient) = await CreateProductWithStockAsync(10);
        var order = await CreateOrderAsync(adminClient, variantId, 2);

        var (secondVariantId, _) = await CreateProductWithStockAsync(10, price: 500m);
        var addResponse = await adminClient.PostAsJsonAsync(
            $"/api/orders/{order.Id}/items", new AddOrderItemRequest(secondVariantId, 1), JsonOptions);
        var afterAdd = await addResponse.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);
        var itemToRemove = afterAdd!.Items.Single(i => i.ProductVariantId == secondVariantId);

        var removeResponse = await adminClient.DeleteAsync($"/api/orders/{order.Id}/items/{itemToRemove.Id}");
        removeResponse.EnsureSuccessStatusCode();
        var updated = await removeResponse.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        Assert.Single(updated!.Items);
        Assert.Equal(2000m, updated.Subtotal); // back to just the original 2 units

        var inventory = await (await adminClient.GetAsync($"/api/inventory/{secondVariantId}")).Content.ReadFromJsonAsync<InventoryDto>();
        Assert.Equal(0, inventory!.ReservedQuantity);
        Assert.Equal(10, inventory.AvailableQuantity);
    }

    [Fact]
    public async Task RemoveItem_LastRemainingItem_ReturnsBadRequest()
    {
        var (variantId, adminClient) = await CreateProductWithStockAsync(10);
        var order = await CreateOrderAsync(adminClient, variantId, 1);

        var response = await adminClient.DeleteAsync($"/api/orders/{order.Id}/items/{order.Items[0].Id}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateNegotiation_SetsDiscountAndFreeShipping()
    {
        var (variantId, adminClient) = await CreateProductWithStockAsync(10);
        var order = await CreateOrderAsync(adminClient, variantId, 1);

        var response = await adminClient.PutAsJsonAsync(
            $"/api/orders/{order.Id}/negotiation", new UpdateOrderNegotiationRequest(300m, true), JsonOptions);
        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        Assert.Equal(300m, updated!.ManualDiscountAmount);
        Assert.True(updated.NegotiatedFreeShipping);
        Assert.Equal(0m, updated.ShippingCost);
        Assert.Equal(900m, updated.DiscountTotal); // 300 (manual) + 600 (Alger home-delivery shipping)
        Assert.Equal(100m, updated.Total); // 1000 - 900 + 0
    }

    [Fact]
    public async Task UpdateNegotiation_ThenAddItem_RecalculatesWithNegotiationStillApplied()
    {
        var (variantId, adminClient) = await CreateProductWithStockAsync(10);
        var order = await CreateOrderAsync(adminClient, variantId, 1);

        await adminClient.PutAsJsonAsync($"/api/orders/{order.Id}/negotiation", new UpdateOrderNegotiationRequest(null, true), JsonOptions);

        var (secondVariantId, _) = await CreateProductWithStockAsync(10, price: 500m);
        var response = await adminClient.PostAsJsonAsync(
            $"/api/orders/{order.Id}/items", new AddOrderItemRequest(secondVariantId, 1), JsonOptions);
        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        Assert.True(updated!.NegotiatedFreeShipping);
        Assert.Equal(0m, updated.ShippingCost);
        Assert.Equal(1500m, updated.Subtotal); // 1000 + 500
    }

    [Fact]
    public async Task UpdateNotes_PersistsRegardlessOfStatus()
    {
        var (variantId, adminClient) = await CreateProductWithStockAsync(10);
        var order = await CreateOrderAsync(adminClient, variantId, 1);

        var cancelResponse = await adminClient.PostAsJsonAsync(
            $"/api/orders/{order.Id}/status", new ChangeOrderStatusRequest(OrderStatus.Cancelled, "test"), JsonOptions);
        cancelResponse.EnsureSuccessStatusCode();

        var response = await adminClient.PutAsJsonAsync(
            $"/api/orders/{order.Id}/notes", new UpdateOrderNotesRequest("Client veut être rappelé demain."), JsonOptions);
        response.EnsureSuccessStatusCode();
        var updated = await response.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        Assert.Equal("Client veut être rappelé demain.", updated!.Notes);
    }

    [Fact]
    public async Task AddItem_CouponAppliedAtCreation_StillAppliesAfterEdit()
    {
        var (variantId, adminClient) = await CreateProductWithStockAsync(10, price: 1000m);
        var couponCode = $"SAVE10-{Guid.NewGuid():N}"[..12].ToUpperInvariant();

        var couponResponse = await adminClient.PostAsJsonAsync("/api/promotions", new SavePromotionRequest(
            "Coupon test", null, PromotionType.Coupon, null, 100m, null, null, couponCode,
            DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddDays(1), true, 0, [], []), JsonOptions);
        couponResponse.EnsureSuccessStatusCode();

        var guestClient = factory.CreateClient();
        var orderResponse = await guestClient.PostAsJsonAsync("/api/orders", new CreateOrderRequest(
            "Amina", "Benali", "0551234567", "Alger", "Bab Ezzouar", "12 rue des Frères",
            DeliveryType.HomeDelivery, null, [new OrderItemRequest(variantId, 1)], couponCode), JsonOptions);
        orderResponse.EnsureSuccessStatusCode();
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        Assert.Equal(100m, order!.DiscountTotal);

        var (secondVariantId, _) = await CreateProductWithStockAsync(10, price: 500m);
        var addResponse = await adminClient.PostAsJsonAsync(
            $"/api/orders/{order.Id}/items", new AddOrderItemRequest(secondVariantId, 1), JsonOptions);
        addResponse.EnsureSuccessStatusCode();
        var updated = await addResponse.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        // The coupon's 100 DA discount must still be present after the edit, not silently dropped.
        Assert.Equal(1500m, updated!.Subtotal);
        Assert.Equal(100m, updated.DiscountTotal);
        Assert.Contains(updated.AppliedPromotions, p => p.PromotionName == "Coupon test");
    }
}
