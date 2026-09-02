namespace Ecommerce.Application.Shipping;

/// <summary>
/// CLAUDE.md section 17: ZR Express must not be assumed to share Yalidine's auth/endpoint shape.
/// Field names here are placeholders pending the merchant's actual API contract — see
/// ZRExpressShippingProvider for what's built vs. deliberately left unimplemented.
/// </summary>
public class ZRExpressOptions
{
    public const string SectionName = "ZRExpress";

    public string BaseUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(ApiKey);
}
