using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Catalog;

public class Product : Entity
{
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;

    // Per-product marketing pixels (CLAUDE.md section 21) — optional overrides fired in addition to
    // the site-wide VITE_META_PIXEL_ID/VITE_TIKTOK_PIXEL_ID, for merchants running a separate ad
    // account/campaign per product. Null means "use only the site-wide pixel".
    public string? FacebookPixelId { get; set; }
    public string? TikTokPixelId { get; set; }

    public Category Category { get; set; } = null!;
    public ICollection<ProductVariant> Variants { get; set; } = [];
    public ICollection<ProductImage> Images { get; set; } = [];
}
