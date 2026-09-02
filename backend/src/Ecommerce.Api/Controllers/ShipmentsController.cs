using Ecommerce.Application.Shipping;
using Ecommerce.Application.Shipping.Dtos;
using Ecommerce.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/shipments")]
[Authorize(Roles = Roles.OrderManagers)]
public class ShipmentsController(IShippingService shippingService) : ControllerBase
{
    [HttpPost("{id:guid}/sync")]
    public async Task<ActionResult<ShipmentDto>> Sync(Guid id, CancellationToken cancellationToken)
    {
        var shipment = await shippingService.SyncTrackingAsync(id, cancellationToken);
        return Ok(shipment);
    }

    [HttpGet("{id:guid}/label")]
    public async Task<ActionResult> GetLabel(Guid id, CancellationToken cancellationToken)
    {
        var label = await shippingService.GetLabelAsync(id, cancellationToken);
        return Content(label, "text/plain");
    }
}
