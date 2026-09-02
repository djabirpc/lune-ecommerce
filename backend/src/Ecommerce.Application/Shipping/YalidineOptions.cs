namespace Ecommerce.Application.Shipping;

/// <summary>
/// CLAUDE.md section 16: credentials only, never endpoints — the actual Yalidine API contract
/// (endpoints, request/response shapes) is not implemented because no official documentation was
/// available. See YalidineShippingProvider for what's built vs. deliberately left unimplemented.
/// </summary>
public class YalidineOptions
{
    public const string SectionName = "Yalidine";

    public string BaseUrl { get; set; } = string.Empty;
    public string ApiId { get; set; } = string.Empty;
    public string ApiToken { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(ApiId) && !string.IsNullOrWhiteSpace(ApiToken);
}
