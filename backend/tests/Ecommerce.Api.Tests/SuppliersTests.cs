using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ecommerce.Application.Auth.Dtos;
using Ecommerce.Application.Common;
using Ecommerce.Application.Suppliers.Dtos;

namespace Ecommerce.Api.Tests;

public class SuppliersTests(AuthWebApplicationFactory factory) : IClassFixture<AuthWebApplicationFactory>
{
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
    public async Task CreateSupplier_ReturnsIt()
    {
        var client = await CreateAuthenticatedClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];

        var response = await client.PostAsJsonAsync("/api/suppliers", new SaveSupplierRequest(
            $"Textile Import {unique}", "0551234567", "contact@textile.dz", "Zone industrielle, Alger", "Livraison sous 15 jours", true));
        response.EnsureSuccessStatusCode();
        var supplier = await response.Content.ReadFromJsonAsync<SupplierDto>();

        Assert.Equal($"Textile Import {unique}", supplier!.Name);
        Assert.Equal("0551234567", supplier.Phone);
        Assert.True(supplier.IsActive);
    }

    [Fact]
    public async Task CreateSupplier_WithInvalidEmail_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];

        var response = await client.PostAsJsonAsync("/api/suppliers", new SaveSupplierRequest(
            $"Fournisseur {unique}", null, "not-an-email", null, null, true));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateSupplier_CanRenameAndDeactivate()
    {
        var client = await CreateAuthenticatedClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];

        var createResponse = await client.PostAsJsonAsync("/api/suppliers", new SaveSupplierRequest(
            $"Fournisseur {unique}", null, null, null, null, true));
        var created = await createResponse.Content.ReadFromJsonAsync<SupplierDto>();

        var updateResponse = await client.PutAsJsonAsync($"/api/suppliers/{created!.Id}", new SaveSupplierRequest(
            $"Fournisseur Renommé {unique}", "0559998877", null, null, "Ne livre plus", false));
        updateResponse.EnsureSuccessStatusCode();
        var updated = await updateResponse.Content.ReadFromJsonAsync<SupplierDto>();

        Assert.Equal($"Fournisseur Renommé {unique}", updated!.Name);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task GetPaged_ExcludesInactiveSuppliersByDefault()
    {
        var client = await CreateAuthenticatedClientAsync();
        var unique = Guid.NewGuid().ToString("N")[..8];

        var createResponse = await client.PostAsJsonAsync("/api/suppliers", new SaveSupplierRequest(
            $"Fournisseur Inactif {unique}", null, null, null, null, true));
        var created = await createResponse.Content.ReadFromJsonAsync<SupplierDto>();
        await client.PutAsJsonAsync($"/api/suppliers/{created!.Id}", new SaveSupplierRequest(created.Name, null, null, null, null, false));

        var activeOnlyResponse = await client.GetAsync("/api/suppliers?pageSize=100");
        var activeOnly = await activeOnlyResponse.Content.ReadFromJsonAsync<PagedResult<SupplierDto>>();
        Assert.DoesNotContain(activeOnly!.Items, s => s.Id == created.Id);

        var includeInactiveResponse = await client.GetAsync("/api/suppliers?includeInactive=true&pageSize=100");
        var includeInactive = await includeInactiveResponse.Content.ReadFromJsonAsync<PagedResult<SupplierDto>>();
        Assert.Contains(includeInactive!.Items, s => s.Id == created.Id);
    }

    [Fact]
    public async Task CreateSupplier_WithoutAuth_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/suppliers", new SaveSupplierRequest("Fournisseur", null, null, null, null, true));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
