using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Marketing;
using Ecommerce.Application.Marketing.Dtos;
using Ecommerce.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

public class UploadHomeBannerForm
{
    public IFormFile? File { get; set; }
    public string? LinkUrl { get; set; }
}

[ApiController]
[Route("api/home-banners")]
public class HomeBannersController(IHomeBannerService homeBannerService) : ControllerBase
{
    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<HomeBannerDto>>> GetActive(CancellationToken cancellationToken)
    {
        var banners = await homeBannerService.GetActiveAsync(cancellationToken);
        return Ok(banners);
    }

    [HttpGet]
    [Authorize(Roles = Roles.MarketingManagers)]
    public async Task<ActionResult<IReadOnlyList<HomeBannerDto>>> GetAll(CancellationToken cancellationToken)
    {
        var banners = await homeBannerService.GetAllAsync(cancellationToken);
        return Ok(banners);
    }

    [HttpPost]
    [Authorize(Roles = Roles.MarketingManagers)]
    [RequestSizeLimit(10_000_000)]
    public async Task<ActionResult<HomeBannerDto>> Add([FromForm] UploadHomeBannerForm form, CancellationToken cancellationToken)
    {
        if (form.File is null)
        {
            throw new ValidationAppException("Aucun fichier n'a été fourni.");
        }

        await using var stream = form.File.OpenReadStream();
        var uploadRequest = new UploadFileRequest(stream, form.File.FileName, form.File.ContentType, form.File.Length);
        var banner = await homeBannerService.AddAsync(uploadRequest, form.LinkUrl, cancellationToken);
        return Ok(banner);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.MarketingManagers)]
    public async Task<ActionResult<HomeBannerDto>> Update(Guid id, UpdateHomeBannerRequest request, CancellationToken cancellationToken)
    {
        var banner = await homeBannerService.UpdateAsync(id, request, cancellationToken);
        return Ok(banner);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.MarketingManagers)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await homeBannerService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
