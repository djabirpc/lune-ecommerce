namespace Ecommerce.Application.Shipping;

/// <summary>
/// 48h Express runs on the white-label "Ecotrack" API. BaseUrl and ApiToken are account-specific —
/// pulled from the merchant's own 48h Express dashboard, never hardcoded (CLAUDE.md section 16's
/// "never guess credentials" applies here too, even though the endpoint shapes themselves are
/// documented — see Ecotrack48hShippingProvider).
/// </summary>
public class Ecotrack48hOptions
{
    public const string SectionName = "Ecotrack48h";

    public string BaseUrl { get; set; } = string.Empty;
    public string ApiToken { get; set; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(ApiToken);
}
