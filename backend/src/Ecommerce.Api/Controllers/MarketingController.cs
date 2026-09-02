using Ecommerce.Application.Marketing;
using Ecommerce.Application.Marketing.Dtos;
using Ecommerce.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/marketing")]
[Authorize(Roles = Roles.MarketingManagers)]
public class MarketingController(IMarketingService marketingService) : ControllerBase
{
    [HttpGet("sources")]
    public async Task<ActionResult<IReadOnlyList<MarketingSourceSummaryDto>>> GetSources(
        [FromQuery] int days = 30,
        CancellationToken cancellationToken = default)
    {
        var summary = await marketingService.GetSourceSummaryAsync(days, cancellationToken);
        return Ok(summary);
    }
}
