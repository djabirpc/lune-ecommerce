using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ecommerce.Application.Auth.Dtos;
using Ecommerce.Application.Catalog.Dtos;
using Ecommerce.Application.Dashboard.Dtos;
using Ecommerce.Application.Orders.Dtos;
using Ecommerce.Domain.Orders;

namespace Ecommerce.Api.Tests;

public class DashboardTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
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

    private async Task<(Guid VariantId, HttpClient AdminClient, string ProductName)> CreateProductWithStockAsync(int initialQuantity)
    {
        var adminClient = await CreateAuthenticatedClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];
        var productName = $"Dashboard Test Product {unique}";

        var categoryResponse = await adminClient.PostAsJsonAsync("/api/categories", new CreateCategoryRequest($"Cat {unique}", $"cat-{unique}", null, 0));
        var category = await categoryResponse.Content.ReadFromJsonAsync<CategoryDto>();

        var productResponse = await adminClient.PostAsJsonAsync("/api/products", new CreateProductRequest(
            category!.Id, productName, $"produit-{unique}", null, 2000m,
            [new CreateProductVariantRequest("Noir", "M", $"SKU-{unique}", null, initialQuantity)]));
        var product = await productResponse.Content.ReadFromJsonAsync<ProductDetailDto>();

        return (product!.Variants.Single().Id, adminClient, productName);
    }

    private static CreateOrderRequest BuildOrderRequest(Guid variantId, int quantity) => new(
        "Amina", "Benali", "0551234567", "Alger", "Bab Ezzouar", "12 rue des Frères",
        DeliveryType.HomeDelivery, null, [new OrderItemRequest(variantId, quantity)]);

    private static string TodayApiDate() => DateTime.UtcNow.ToString("yyyy-MM-dd");

    [Fact]
    public async Task GetOverview_Unauthenticated_ReturnsUnauthorized()
    {
        var guestClient = factory.CreateClient();

        var response = await guestClient.GetAsync($"/api/dashboard/overview?from={TodayApiDate()}&to={TodayApiDate()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetOverview_AfterDeliveredOrder_ReflectsRealDataAcrossSections()
    {
        var (variantId, adminClient, productName) = await CreateProductWithStockAsync(initialQuantity: 5);
        var guestClient = factory.CreateClient();

        var orderResponse = await guestClient.PostAsJsonAsync("/api/orders", BuildOrderRequest(variantId, 2), JsonOptions);
        orderResponse.EnsureSuccessStatusCode();
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);

        foreach (var status in new[] { OrderStatus.Confirmed, OrderStatus.ReadyToShip, OrderStatus.Shipped, OrderStatus.OutForDelivery, OrderStatus.Delivered })
        {
            var response = await adminClient.PostAsJsonAsync($"/api/orders/{order!.Id}/status", new ChangeOrderStatusRequest(status, null), JsonOptions);
            response.EnsureSuccessStatusCode();
            order = await response.Content.ReadFromJsonAsync<OrderDetailDto>(JsonOptions);
        }

        var today = TodayApiDate();
        var overviewResponse = await adminClient.GetAsync($"/api/dashboard/overview?from={today}&to={today}");
        overviewResponse.EnsureSuccessStatusCode();
        var overview = await overviewResponse.Content.ReadFromJsonAsync<DashboardOverviewDto>(JsonOptions);

        Assert.True(overview!.Kpis.Delivered.Value >= 1);
        Assert.True(overview.Kpis.Revenue.Value >= 4600m); // 2 x 2000 DA + 600 DA Alger home-delivery shipping
        Assert.Contains(overview.TopProducts, p => p.ProductName == productName && p.UnitsSold == 2);
        Assert.Contains(overview.Funnel, f => f.Key == "delivered" && f.Count >= 1);
        Assert.All(overview.ActionQueue, item => Assert.False(string.IsNullOrWhiteSpace(item.Label)));
    }

    [Fact]
    public async Task GetOverview_FuturePeriodWithNoData_ReturnsZeroesWithoutError()
    {
        var adminClient = await CreateAuthenticatedClientAsync();

        var response = await adminClient.GetAsync("/api/dashboard/overview?from=2099-01-01&to=2099-01-31");
        response.EnsureSuccessStatusCode();
        var overview = await response.Content.ReadFromJsonAsync<DashboardOverviewDto>(JsonOptions);

        Assert.Equal(0m, overview!.Kpis.OrdersCreated.Value);
        Assert.Empty(overview.TopProducts);
    }
}
