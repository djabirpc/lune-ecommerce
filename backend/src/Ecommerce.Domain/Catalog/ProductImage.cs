using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Catalog;

public class ProductImage : Entity
{
    public Guid ProductId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPrimary { get; set; }

    public Product Product { get; set; } = null!;
}
