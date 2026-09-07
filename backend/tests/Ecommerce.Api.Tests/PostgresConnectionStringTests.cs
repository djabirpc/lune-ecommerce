using Ecommerce.Infrastructure.Persistence;

namespace Ecommerce.Api.Tests;

public class PostgresConnectionStringTests
{
    [Fact]
    public void Normalize_AdoNetFormat_PassesThroughUnchanged()
    {
        const string connectionString = "Host=localhost;Port=5432;Database=luna;Username=luna;Password=secret";

        var result = PostgresConnectionString.Normalize(connectionString);

        Assert.Equal(connectionString, result);
    }

    [Theory]
    [InlineData("postgres://luna:secret@db.render.com:5432/luna_db")]
    [InlineData("postgresql://luna:secret@db.render.com:5432/luna_db")]
    public void Normalize_UriFormat_ConvertsToAdoNetFormat(string uri)
    {
        var result = PostgresConnectionString.Normalize(uri);

        Assert.Contains("Host=db.render.com", result);
        Assert.Contains("Port=5432", result);
        Assert.Contains("Database=luna_db", result);
        Assert.Contains("Username=luna", result);
        Assert.Contains("Password=secret", result);
        Assert.Contains("SSL Mode=Require", result);
    }

    [Fact]
    public void Normalize_UriFormat_WithUrlEncodedPassword_DecodesCorrectly()
    {
        var result = PostgresConnectionString.Normalize("postgres://luna:p%40ss%23w0rd@db.render.com:5432/luna_db");

        Assert.Contains("Password=p@ss#w0rd", result);
    }

    [Fact]
    public void Normalize_UriFormat_WithoutExplicitPort_DefaultsTo5432()
    {
        var result = PostgresConnectionString.Normalize("postgres://luna:secret@db.render.com/luna_db");

        Assert.Contains("Port=5432", result);
    }
}
