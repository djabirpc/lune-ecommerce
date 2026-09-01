using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ecommerce.Application.Auth.Dtos;
using Ecommerce.Application.Catalog.Dtos;
using Ecommerce.Application.Orders.Dtos;
using Ecommerce.Domain.Orders;

namespace Ecommerce.Api.Tests;

public class OrderCallAttemptTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
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

    private async Task<OrderDetailDto> CreatePendingOrderAsync(HttpClient adminClient, HttpClient guestClient)
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
        return (await orderResponse.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions))!;
    }

    [Fact]
    public async Task RecordNoAnswerAttempt_LeavesStatusUnchanged_AndAppendsCallAttempt()
    {
        var adminClient = await CreateAuthenticatedClientAsync();
        var guestClient = factory.CreateClient();
        var order = await CreatePendingOrderAsync(adminClient, guestClient);

        var response = await adminClient.PostAsJsonAsync(
            $"/api/orders/{order.Id}/call-attempts",
            new RecordCallAttemptRequest(CallAttemptResult.NoAnswer, "Pas de réponse", null),
            JsonOptions);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);
        Assert.Equal(OrderStatus.PendingConfirmation, updated!.Status);
        Assert.Single(updated.CallAttempts);
        Assert.Equal(1, updated.CallAttempts[0].AttemptNumber);
        Assert.Equal(CallAttemptResult.NoAnswer, updated.CallAttempts[0].Result);
    }

    [Fact]
    public async Task RecordConfirmedAttempt_TransitionsOrderToConfirmed_AndRecordsHistoryAndAttempt()
    {
        var adminClient = await CreateAuthenticatedClientAsync();
        var guestClient = factory.CreateClient();
        var order = await CreatePendingOrderAsync(adminClient, guestClient);

        var response = await adminClient.PostAsJsonAsync(
            $"/api/orders/{order.Id}/call-attempts",
            new RecordCallAttemptRequest(CallAttemptResult.Confirmed, "Client confirme", null),
            JsonOptions);
        response.EnsureSuccessStatusCode();

        var updated = await response.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);
        Assert.Equal(OrderStatus.Confirmed, updated!.Status);
        Assert.Single(updated.CallAttempts);
        Assert.Contains(updated.StatusHistory, h => h.NewStatus == OrderStatus.Confirmed);
    }

    [Fact]
    public async Task RecordCallbackScheduled_WithoutNextCallAt_ReturnsBadRequest()
    {
        var adminClient = await CreateAuthenticatedClientAsync();
        var guestClient = factory.CreateClient();
        var order = await CreatePendingOrderAsync(adminClient, guestClient);

        var response = await adminClient.PostAsJsonAsync(
            $"/api/orders/{order.Id}/call-attempts",
            new RecordCallAttemptRequest(CallAttemptResult.CallbackScheduled, null, null),
            JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RecordAttempt_OnOrderNotAwaitingConfirmation_ReturnsConflict()
    {
        var adminClient = await CreateAuthenticatedClientAsync();
        var guestClient = factory.CreateClient();
        var order = await CreatePendingOrderAsync(adminClient, guestClient);

        var confirmResponse = await adminClient.PostAsJsonAsync(
            $"/api/orders/{order.Id}/status",
            new ChangeOrderStatusRequest(OrderStatus.Confirmed, null),
            JsonOptions);
        confirmResponse.EnsureSuccessStatusCode();

        var response = await adminClient.PostAsJsonAsync(
            $"/api/orders/{order.Id}/call-attempts",
            new RecordCallAttemptRequest(CallAttemptResult.NoAnswer, null, null),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
