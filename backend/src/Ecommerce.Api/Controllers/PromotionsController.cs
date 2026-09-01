using Ecommerce.Application.Common;
using Ecommerce.Application.Promotions;
using Ecommerce.Application.Promotions.Dtos;
using Ecommerce.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/promotions")]
public class PromotionsController(IPromotionService promotionService) : ControllerBase
{
    [HttpGet("active")]
    [AllowAnonymous]
    public async Task<ActionResult<IReadOnlyList<PromotionDto>>> GetActive(CancellationToken cancellationToken)
    {
        var promotions = await promotionService.GetActiveAsync(cancellationToken);
        return Ok(promotions);
    }

    [HttpGet]
    [Authorize(Roles = Roles.PromotionManagers)]
    public async Task<ActionResult<PagedResult<PromotionDto>>> GetPaged(
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await promotionService.GetPagedAsync(includeInactive, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = Roles.PromotionManagers)]
    public async Task<ActionResult<PromotionDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var promotion = await promotionService.GetByIdAsync(id, cancellationToken);
        return Ok(promotion);
    }

    [HttpPost]
    [Authorize(Roles = Roles.PromotionManagers)]
    public async Task<ActionResult<PromotionDetailDto>> Create(SavePromotionRequest request, CancellationToken cancellationToken)
    {
        var promotion = await promotionService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = promotion.Id }, promotion);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.PromotionManagers)]
    public async Task<ActionResult<PromotionDetailDto>> Update(Guid id, SavePromotionRequest request, CancellationToken cancellationToken)
    {
        var promotion = await promotionService.UpdateAsync(id, request, cancellationToken);
        return Ok(promotion);
    }
}
