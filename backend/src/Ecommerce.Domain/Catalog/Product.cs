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

    public Category Category { get; set; } = null!;
    public ICollection<ProductVariant> Variants { get; set; } = [];
    public ICollection<ProductImage> Images { get; set; } = [];
}
