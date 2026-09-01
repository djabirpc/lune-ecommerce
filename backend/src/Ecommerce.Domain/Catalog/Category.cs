using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Catalog;

public class Category : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }

    public ICollection<Product> Products { get; set; } = [];
}
