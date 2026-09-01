using Ecommerce.Application.Catalog;
using Ecommerce.Application.Catalog.Dtos;
using Ecommerce.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetAll(CancellationToken cancellationToken)
    {
        var categories = await categoryService.GetAllAsync(includeInactive: false, cancellationToken);
        return Ok(categories);
    }

    [HttpGet("{slug}")]
    [AllowAnonymous]
    public async Task<ActionResult<CategoryDto>> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var category = await categoryService.GetBySlugAsync(slug, cancellationToken);
        return Ok(category);
    }

    [HttpPost]
    [Authorize(Roles = Roles.CatalogManagers)]
    public async Task<ActionResult<CategoryDto>> Create(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await categoryService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetBySlug), new { slug = category.Slug }, category);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.CatalogManagers)]
    public async Task<ActionResult<CategoryDto>> Update(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var category = await categoryService.UpdateAsync(id, request, cancellationToken);
        return Ok(category);
    }
}
