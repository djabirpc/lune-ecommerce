using Ecommerce.Application.Shipping;
using Ecommerce.Application.Shipping.Dtos;
using Ecommerce.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/shipping")]
[Authorize(Roles = Roles.OrderManagers)]
public class ShippingController(IShippingService shippingService) : ControllerBase
{
    [HttpGet("carriers")]
    public ActionResult<IReadOnlyList<ShippingCarrierAvailabilityDto>> GetCarriers()
    {
        return Ok(shippingService.GetCarrierAvailability());
    }
}
