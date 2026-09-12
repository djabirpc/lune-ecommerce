namespace Ecommerce.Infrastructure.Persistence;

/// <summary>
/// Some hosting platforms (Railway, Render, Heroku, ...) hand out Postgres credentials as a
/// <c>postgres://user:pass@host:port/db</c> URI rather than an ADO.NET keyword=value string.
/// Npgsql only understands the latter, so this converts one to the other when needed; a
/// connection string that's already in ADO.NET format passes through unchanged.
/// </summary>
public static class PostgresConnectionString
{
    public static string Normalize(string connectionString)
    {
        if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var database = uri.AbsolutePath.TrimStart('/');
        var port = uri.Port == -1 ? 5432 : uri.Port;

        return $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
    }
}
