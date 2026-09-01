using Ecommerce.Application.Catalog;
using Ecommerce.Application.Catalog.Dtos;
using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Domain.Catalog;
using Ecommerce.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Catalog;

public class CategoryService(
    AppDbContext dbContext,
    IValidator<CreateCategoryRequest> createValidator,
    IValidator<UpdateCategoryRequest> updateValidator) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryDto>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Categories.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => ToDto(c))
            .ToListAsync(cancellationToken);
    }

    public async Task<CategoryDto> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.Categories.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Slug == slug, cancellationToken);

        return category is null
            ? throw new NotFoundAppException("Catégorie introuvable.")
            : ToDto(category);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        if (await dbContext.Categories.AnyAsync(c => c.Slug == request.Slug, cancellationToken))
        {
            throw new ConflictAppException("Une catégorie avec ce slug existe déjà.");
        }

        var category = new Category
        {
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            DisplayOrder = request.DisplayOrder,
        };

        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(category);
    }

    public async Task<CategoryDto> UpdateAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var category = await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new NotFoundAppException("Catégorie introuvable.");

        if (await dbContext.Categories.AnyAsync(c => c.Slug == request.Slug && c.Id != id, cancellationToken))
        {
            throw new ConflictAppException("Une catégorie avec ce slug existe déjà.");
        }

        category.Name = request.Name;
        category.Slug = request.Slug;
        category.Description = request.Description;
        category.IsActive = request.IsActive;
        category.DisplayOrder = request.DisplayOrder;
        category.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(category);
    }

    private static CategoryDto ToDto(Category category) => new(
        category.Id,
        category.Name,
        category.Slug,
        category.Description,
        category.IsActive,
        category.DisplayOrder);
}
