using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Marketing;

/// <summary>
/// A slide in the homepage hero carousel (CLAUDE.md section 8). If no active banners exist, the
/// storefront falls back to its original behavior (the newest product's primary photo) — this table
/// is purely additive, never a hard requirement for the homepage to render.
/// </summary>
public class HomeBanner : Entity
{
    public string ImageUrl { get; set; } = string.Empty;

    /// <summary>Optional in-app path the slide links to when clicked (e.g. "/promotions",
    /// "/category/robes"). Null means the slide is purely decorative.</summary>
    public string? LinkUrl { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
