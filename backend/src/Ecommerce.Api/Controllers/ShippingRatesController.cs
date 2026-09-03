using Ecommerce.Application.Shipping;
using Ecommerce.Application.Shipping.Dtos;
using Ecommerce.Domain.Identity;
using Ecommerce.Domain.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/shipping-rates")]
public class ShippingRatesController(IShippingRateService shippingRateService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = Roles.OrderManagers)]
    public async Task<ActionResult<IReadOnlyList<ShippingRateDto>>> GetAll(CancellationToken cancellationToken)
    {
        var rates = await shippingRateService.GetAllAsync(cancellationToken);
        return Ok(rates);
    }

    [HttpPut("{wilaya}")]
    [Authorize(Roles = Roles.OrderManagers)]
    public async Task<ActionResult<ShippingRateDto>> Update(string wilaya, UpdateShippingRateRequest request, CancellationToken cancellationToken)
    {
        var rate = await shippingRateService.UpdateAsync(wilaya, request, cancellationToken);
        return Ok(rate);
    }

    /// <summary>Public checkout-side price preview — the backend recalculates authoritatively again at order creation regardless (CLAUDE.md section 41).</summary>
    [HttpGet("quote")]
    [AllowAnonymous]
    public async Task<ActionResult<ShippingQuoteDto>> GetQuote(
        [FromQuery] string wilaya,
        [FromQuery] DeliveryType deliveryType,
        CancellationToken cancellationToken)
    {
        var price = await shippingRateService.GetPriceAsync(wilaya, deliveryType, cancellationToken);
        return Ok(new ShippingQuoteDto(wilaya, deliveryType, price));
    }
}
