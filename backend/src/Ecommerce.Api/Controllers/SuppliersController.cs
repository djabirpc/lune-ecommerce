using Ecommerce.Application.Common;
using Ecommerce.Application.Suppliers;
using Ecommerce.Application.Suppliers.Dtos;
using Ecommerce.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/suppliers")]
[Authorize(Roles = Roles.CatalogManagers)]
public class SuppliersController(ISupplierService supplierService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<SupplierDto>>> GetPaged(
        [FromQuery] bool includeInactive = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await supplierService.GetPagedAsync(includeInactive, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<SupplierDto>> Create(SaveSupplierRequest request, CancellationToken cancellationToken)
    {
        var supplier = await supplierService.CreateAsync(request, cancellationToken);
        return Ok(supplier);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SupplierDto>> Update(Guid id, SaveSupplierRequest request, CancellationToken cancellationToken)
    {
        var supplier = await supplierService.UpdateAsync(id, request, cancellationToken);
        return Ok(supplier);
    }
}
