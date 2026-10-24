using Ecommerce.Application.Common;
using Ecommerce.Application.Customers;
using Ecommerce.Application.Customers.Dtos;
using Ecommerce.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Authorize(Roles = Roles.OrderManagers)]
public class CustomersController(ICustomerService customerService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<CustomerSummaryDto>>> GetPaged(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await customerService.GetPagedAsync(search, page, pageSize, cancellationToken);
        return Ok(result);
    }
}
