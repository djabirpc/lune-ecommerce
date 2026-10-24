using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ecommerce.Application.Auth.Dtos;
using Ecommerce.Application.Catalog.Dtos;
using Ecommerce.Application.Common;
using Ecommerce.Application.Customers.Dtos;
using Ecommerce.Application.Orders.Dtos;
using Ecommerce.Domain.Orders;

namespace Ecommerce.Api.Tests;

public class CustomersTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
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

    private async Task<Guid> CreateVariantAsync(HttpClient adminClient, decimal price = 2000m)
    {
        var unique = Guid.NewGuid().ToString("N")[..8];
        var categoryResponse = await adminClient.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Cat {unique}", $"cat-{unique}", null, 0));
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>();
        var productResponse = await adminClient.PostAsJsonAsync("/api/products", new CreateProductRequest(
            category!.Id, "Produit test", $"produit-{unique}", null, price,
            [new CreateProductVariantRequest("Noir", "M", $"SKU-{unique}", null, 20)]));
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDetailDto>();
        return product!.Variants.Single().Id;
    }

    private async Task<Guid> PlaceOrderAsync(HttpClient adminClient, Guid variantId, string phone, string firstName, string lastName)
    {
        var guestClient = factory.CreateClient();
        var response = await guestClient.PostAsJsonAsync("/api/orders", new CreateOrderRequest(
            firstName, lastName, phone, "Alger", "Bab Ezzouar", "12 rue des Frères",
            DeliveryType.HomeDelivery, null, [new OrderItemRequest(variantId, 1)]), JsonOptions);
        response.EnsureSuccessStatusCode();
        var order = await response.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);
        return order!.Id;
    }

    [Fact]
    public async Task GetPaged_WithoutAuth_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/customers");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetPaged_GroupsMultipleOrdersBySamePhone_IntoOneCustomer()
    {
        var adminClient = await CreateAuthenticatedClientAsync();
        var variantId = await CreateVariantAsync(adminClient, price: 1000m);
        var phone = $"05{Random.Shared.Next(10000000, 99999999)}";

        var order1 = await PlaceOrderAsync(adminClient, variantId, phone, "Amina", "Benali");
        var order2 = await PlaceOrderAsync(adminClient, variantId, phone, "Amina", "Benali");

        var response = await adminClient.GetAsync($"/api/customers?search={phone}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CustomerSummaryDto>>(JsonOptions);

        var customer = Assert.Single(result!.Items);
        Assert.Equal(phone, customer.Phone);
        Assert.Equal("Amina Benali", customer.FullName);
        Assert.Equal(2, customer.TotalOrders);

        await adminClient.PostAsJsonAsync($"/api/orders/{order1}/status", new ChangeOrderStatusRequest(OrderStatus.Cancelled, null), JsonOptions);
        await adminClient.PostAsJsonAsync($"/api/orders/{order2}/status", new ChangeOrderStatusRequest(OrderStatus.Cancelled, null), JsonOptions);
    }

    [Fact]
    public async Task GetPaged_TracksDeliveredRevenueAndRiskCounts()
    {
        var adminClient = await CreateAuthenticatedClientAsync();
        var variantId = await CreateVariantAsync(adminClient, price: 1500m);
        var phone = $"06{Random.Shared.Next(10000000, 99999999)}";

        var deliveredOrderId = await PlaceOrderAsync(adminClient, variantId, phone, "Sara", "Kaci");
        var unreachableOrderId = await PlaceOrderAsync(adminClient, variantId, phone, "Sara", "Kaci");

        // Drive the first order all the way to Delivered.
        await adminClient.PostAsJsonAsync($"/api/orders/{deliveredOrderId}/status", new ChangeOrderStatusRequest(OrderStatus.Confirmed, null), JsonOptions);
        await adminClient.PostAsJsonAsync($"/api/orders/{deliveredOrderId}/status", new ChangeOrderStatusRequest(OrderStatus.ReadyToShip, null), JsonOptions);
        await adminClient.PostAsJsonAsync($"/api/orders/{deliveredOrderId}/status", new ChangeOrderStatusRequest(OrderStatus.Shipped, null), JsonOptions);
        await adminClient.PostAsJsonAsync($"/api/orders/{deliveredOrderId}/status", new ChangeOrderStatusRequest(OrderStatus.OutForDelivery, null), JsonOptions);
        await adminClient.PostAsJsonAsync($"/api/orders/{deliveredOrderId}/status", new ChangeOrderStatusRequest(OrderStatus.Delivered, null), JsonOptions);

        await adminClient.PostAsJsonAsync($"/api/orders/{unreachableOrderId}/status", new ChangeOrderStatusRequest(OrderStatus.CustomerUnreachable, null), JsonOptions);

        var response = await adminClient.GetAsync($"/api/customers?search={phone}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<CustomerSummaryDto>>(JsonOptions);

        var customer = Assert.Single(result!.Items);
        Assert.Equal(2, customer.TotalOrders);
        Assert.Equal(1, customer.DeliveredOrders);
        Assert.Equal(1, customer.UnreachableCount);
        Assert.Equal(2100m, customer.TotalSpent); // 1500 (item) + 600 (default shipping) — Total is the real COD amount collected
    }

    [Fact]
    public async Task GetPaged_DetailDrillDown_FiltersOrdersByPhone()
    {
        var adminClient = await CreateAuthenticatedClientAsync();
        var variantId = await CreateVariantAsync(adminClient);
        var phoneA = $"07{Random.Shared.Next(10000000, 99999999)}";
        var phoneB = $"07{Random.Shared.Next(10000000, 99999999)}";

        await PlaceOrderAsync(adminClient, variantId, phoneA, "Yasmine", "Hadj");
        await PlaceOrderAsync(adminClient, variantId, phoneB, "Imane", "Rahal");

        var response = await adminClient.GetAsync($"/api/orders?phone={phoneA}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<OrderSummaryDto>>(JsonOptions);

        var match = Assert.Single(result!.Items);
        Assert.Equal(phoneA, match.Phone);
    }
}
