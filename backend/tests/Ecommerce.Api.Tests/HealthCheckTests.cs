using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Ecommerce.Api.Tests;

public class HealthCheckTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthCheckTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=luna_test;Username=luna;Password=luna_dev_password",
                    ["Jwt:Issuer"] = "Luna.Api.Tests",
                    ["Jwt:Audience"] = "Luna.Client.Tests",
                    ["Jwt:Key"] = "test-only-jwt-signing-key-not-for-production-use"
                });
            });
        });
    }

    [Fact]
    public async Task Ping_ReturnsSuccess()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/system/ping");

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task HealthEndpoint_RespondsWithoutThrowing()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.True(
            response.StatusCode is HttpStatusCode.OK or HttpStatusCode.ServiceUnavailable,
            $"Unexpected status code: {response.StatusCode}");
    }
}
