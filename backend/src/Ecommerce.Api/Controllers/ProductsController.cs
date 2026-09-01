using Ecommerce.Application.Catalog;
using Ecommerce.Application.Catalog.Dtos;
using Ecommerce.Application.Common;
using Ecommerce.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController(IProductService productService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<ProductListItemDto>>> GetPaged(
        [FromQuery] string? category,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var result = await productService.GetPagedAsync(category, page, pageSize, includeInactive, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{slug}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductDetailDto>> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var product = await productService.GetBySlugAsync(slug, cancellationToken);
        return Ok(product);
    }

    [HttpPost]
    [Authorize(Roles = Roles.CatalogManagers)]
    public async Task<ActionResult<ProductDetailDto>> Create(CreateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await productService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetBySlug), new { slug = product.Slug }, product);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.CatalogManagers)]
    public async Task<ActionResult<ProductDetailDto>> Update(Guid id, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await productService.UpdateAsync(id, request, cancellationToken);
        return Ok(product);
    }

    [HttpPost("{id:guid}/variants")]
    [Authorize(Roles = Roles.CatalogManagers)]
    public async Task<ActionResult<ProductVariantDto>> AddVariant(
        Guid id,
        CreateProductVariantRequest request,
        CancellationToken cancellationToken)
    {
        var variant = await productService.AddVariantAsync(id, request, cancellationToken);
        return Ok(variant);
    }
}
