using Ecommerce.Application.Shipping;
using Ecommerce.Application.Shipping.Dtos;
using Ecommerce.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

/// <summary>Admin-entered reference rates per carrier — never used to compute real order pricing, see
/// CarrierShippingRate's doc comment.</summary>
[ApiController]
[Route("api/carrier-shipping-rates")]
[Authorize(Roles = Roles.OrderManagers)]
public class CarrierShippingRatesController(ICarrierShippingRateService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CarrierShippingRateDto>>> GetAll(CancellationToken cancellationToken)
    {
        return Ok(await service.GetAllAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<CarrierShippingRateDto>> Create(SaveCarrierShippingRateRequest request, CancellationToken cancellationToken)
    {
        var rate = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetAll), new { }, rate);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<CarrierShippingRateDto>> Update(Guid id, SaveCarrierShippingRateRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.UpdateAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
