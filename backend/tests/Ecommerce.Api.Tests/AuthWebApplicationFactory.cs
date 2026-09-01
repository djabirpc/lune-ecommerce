using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Testcontainers.PostgreSql;

namespace Ecommerce.Api.Tests;

public class AuthWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@luna.test";
    public const string AdminPassword = "Test-Admin-Password-123!";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine").Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = _postgres.GetConnectionString(),
                ["Jwt:Issuer"] = "Luna.Api.Tests",
                ["Jwt:Audience"] = "Luna.Client.Tests",
                ["Jwt:Key"] = "test-only-jwt-signing-key-not-for-production-use",
                ["Jwt:AccessTokenExpiryMinutes"] = "60",
                ["Jwt:RefreshTokenExpiryDays"] = "7",
                ["ApplyMigrationsOnStartup"] = "true",
                ["InitialAdmin:Email"] = AdminEmail,
                ["InitialAdmin:Password"] = AdminPassword,
                ["InitialAdmin:FirstName"] = "Test",
                ["InitialAdmin:LastName"] = "Admin",
            });
        });
    }
}
