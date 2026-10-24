using Ecommerce.Application.Marketing;
using Ecommerce.Application.Marketing.Dtos;
using Ecommerce.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

/// <summary>General storefront content (social links, contact phone, announcement bar) — separate
/// from MarketingController's pixel-event settings since nothing here is secret and the read side is
/// fully public, and because this is general site config (Admin/SuperAdmin) rather than a marketing
/// concern (MarketingManager).</summary>
[ApiController]
[Route("api/store-settings")]
public class StoreSettingsController(IStoreSettingsService storeSettingsService) : ControllerBase
{
    /// <summary>Public — the storefront layout calls this on every load to render the announcement
    /// bar, footer social links, and contact phone.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<StoreSettingsDto>> Get(CancellationToken cancellationToken = default) =>
        Ok(await storeSettingsService.GetAsync(cancellationToken));

    [HttpPut]
    [Authorize(Roles = Roles.UserManagers)]
    public async Task<ActionResult<StoreSettingsDto>> Update(
        [FromBody] UpdateStoreSettingsRequest request,
        CancellationToken cancellationToken = default) =>
        Ok(await storeSettingsService.UpdateAsync(request, cancellationToken));
}
