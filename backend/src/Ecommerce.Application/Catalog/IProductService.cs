using Ecommerce.Application.Catalog.Dtos;
using Ecommerce.Application.Common;

namespace Ecommerce.Application.Catalog;

public interface IProductService
{
    Task<PagedResult<ProductListItemDto>> GetPagedAsync(
        string? categorySlug,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ProductDetailDto> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<ProductDetailDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);

    Task<ProductDetailDto> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);

    Task<ProductVariantDto> AddVariantAsync(
        Guid productId,
        CreateProductVariantRequest request,
        CancellationToken cancellationToken = default);
}
