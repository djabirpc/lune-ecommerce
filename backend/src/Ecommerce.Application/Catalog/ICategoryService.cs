using Ecommerce.Application.Catalog.Dtos;

namespace Ecommerce.Application.Catalog;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken = default);

    Task<CategoryDto> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);

    Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);

    Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
}
