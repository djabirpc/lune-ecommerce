using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ecommerce.Application.Auth.Dtos;
using Ecommerce.Application.Shipping.Dtos;
using Ecommerce.Domain.Shipping;

namespace Ecommerce.Api.Tests;

public class CarrierShippingRateTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
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
    public async Task Create_Update_Delete_RoundTrip()
    {
        var adminClient = await CreateAuthenticatedClientAsync();

        var createResponse = await adminClient.PostAsJsonAsync(
            "/api/carrier-shipping-rates",
            new SaveCarrierShippingRateRequest(ShippingCarrier.Ecotrack48h, "Adrar", 700m, 500m, "Note test"),
            JsonOptions);
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<CarrierShippingRateDto>(JsonOptions);
        Assert.Equal(700m, created!.HomeDeliveryPrice);

        var updateResponse = await adminClient.PutAsJsonAsync(
            $"/api/carrier-shipping-rates/{created.Id}",
            new SaveCarrierShippingRateRequest(ShippingCarrier.Ecotrack48h, "Adrar", 800m, 550m, "Note modifiée"),
            JsonOptions);
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<CarrierShippingRateDto>(JsonOptions);
        Assert.Equal(800m, updated!.HomeDeliveryPrice);
        Assert.Equal("Note modifiée", updated.Notes);

        var deleteResponse = await adminClient.DeleteAsync($"/api/carrier-shipping-rates/{created.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var listResponse = await adminClient.GetAsync("/api/carrier-shipping-rates");
        var list = await listResponse.Content.ReadFromJsonAsync<List<CarrierShippingRateDto>>(JsonOptions) ?? [];
        Assert.DoesNotContain(list, r => r.Id == created.Id);
    }

    [Fact]
    public async Task Create_DuplicateCarrierAndWilaya_ReturnsConflict()
    {
        var adminClient = await CreateAuthenticatedClientAsync();

        var first = await adminClient.PostAsJsonAsync(
            "/api/carrier-shipping-rates",
            new SaveCarrierShippingRateRequest(ShippingCarrier.Yalidine, "Oran", 500m, 350m, null),
            JsonOptions);
        first.EnsureSuccessStatusCode();

        var duplicate = await adminClient.PostAsJsonAsync(
            "/api/carrier-shipping-rates",
            new SaveCarrierShippingRateRequest(ShippingCarrier.Yalidine, "Oran", 550m, 400m, null),
            JsonOptions);

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [Fact]
    public async Task Create_UnknownWilaya_ReturnsBadRequest()
    {
        var adminClient = await CreateAuthenticatedClientAsync();

        var response = await adminClient.PostAsJsonAsync(
            "/api/carrier-shipping-rates",
            new SaveCarrierShippingRateRequest(ShippingCarrier.ZRExpress, "Atlantide", 500m, 350m, null),
            JsonOptions);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_WithoutAuth_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/carrier-shipping-rates");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
