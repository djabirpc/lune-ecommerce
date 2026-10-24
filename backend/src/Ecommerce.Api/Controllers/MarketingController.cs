using Ecommerce.Application.Marketing;
using Ecommerce.Application.Marketing.Dtos;
using Ecommerce.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/marketing")]
[Authorize(Roles = Roles.MarketingManagers)]
public class MarketingController(IMarketingService marketingService, IMarketingSettingsService marketingSettingsService) : ControllerBase
{
    [HttpGet("sources")]
    public async Task<ActionResult<IReadOnlyList<MarketingSourceSummaryDto>>> GetSources(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        var summary = await marketingService.GetSourceSummaryAsync(days, cancellationToken);
        return Ok(summary);
    }

    [HttpGet("settings")]
    public async Task<ActionResult<MarketingSettingsDto>> GetSettings(CancellationToken cancellationToken = default) =>
        Ok(await marketingSettingsService.GetAsync(cancellationToken));

    [HttpPut("settings")]
    public async Task<ActionResult<MarketingSettingsDto>> UpdateSettings(
        [FromBody] UpdateMarketingSettingsRequest request,
        CancellationToken cancellationToken = default) =>
        Ok(await marketingSettingsService.UpdateAsync(request, cancellationToken));

    /// <summary>Public — the storefront's checkout page calls this (no auth) to decide whether to fire
    /// the "Purchase" pixel event itself right after creating the order.</summary>
    [HttpGet("purchase-trigger")]
    [AllowAnonymous]
    public async Task<ActionResult<PurchaseTriggerDto>> GetPurchaseTrigger(CancellationToken cancellationToken = default) =>
        Ok(new PurchaseTriggerDto(await marketingSettingsService.GetPurchaseEventTriggerAsync(cancellationToken)));
}
