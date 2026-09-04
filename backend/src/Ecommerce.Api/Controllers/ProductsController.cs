using Ecommerce.Application.Catalog;
using Ecommerce.Application.Catalog.Dtos;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

public class UploadProductImageForm
{
    public IFormFile? File { get; set; }
    public string? AltText { get; set; }
    public bool IsPrimary { get; set; }
}

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
        [FromQuery] bool sortByNewest = false,
        CancellationToken cancellationToken = default)
    {
        var result = await productService.GetPagedAsync(category, page, pageSize, includeInactive, sortByNewest, cancellationToken);
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

    [HttpPost("{id:guid}/images")]
    [Authorize(Roles = Roles.CatalogManagers)]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<ProductImageDto>> AddImage(Guid id, [FromForm] UploadProductImageForm form, CancellationToken cancellationToken)
    {
        if (form.File is null)
        {
            throw new ValidationAppException("Aucun fichier n'a été fourni.");
        }

        await using var stream = form.File.OpenReadStream();
        var uploadRequest = new UploadFileRequest(stream, form.File.FileName, form.File.ContentType, form.File.Length);
        var image = await productService.AddImageAsync(id, uploadRequest, form.AltText, form.IsPrimary, cancellationToken);
        return Ok(image);
    }

    [HttpDelete("{id:guid}/images/{imageId:guid}")]
    [Authorize(Roles = Roles.CatalogManagers)]
    public async Task<IActionResult> DeleteImage(Guid id, Guid imageId, CancellationToken cancellationToken)
    {
        await productService.DeleteImageAsync(id, imageId, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/images/{imageId:guid}/primary")]
    [Authorize(Roles = Roles.CatalogManagers)]
    public async Task<ActionResult<ProductImageDto>> SetPrimaryImage(Guid id, Guid imageId, CancellationToken cancellationToken)
    {
        var image = await productService.SetPrimaryImageAsync(id, imageId, cancellationToken);
        return Ok(image);
    }
}
