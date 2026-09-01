using Ecommerce.Application.Inventory;
using Ecommerce.Application.Inventory.Dtos;
using Ecommerce.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

[ApiController]
[Route("api/inventory")]
[Authorize(Roles = Roles.CatalogManagers)]
public class InventoryController(IInventoryService inventoryService) : ControllerBase
{
    [HttpGet("{variantId:guid}")]
    public async Task<ActionResult<InventoryDto>> GetByVariant(Guid variantId, CancellationToken cancellationToken)
    {
        var inventory = await inventoryService.GetByVariantIdAsync(variantId, cancellationToken);
        return Ok(inventory);
    }

    [HttpGet("{variantId:guid}/transactions")]
    public async Task<ActionResult<IReadOnlyList<InventoryTransactionDto>>> GetTransactions(Guid variantId, CancellationToken cancellationToken)
    {
        var transactions = await inventoryService.GetTransactionsAsync(variantId, cancellationToken);
        return Ok(transactions);
    }

    [HttpPost("restock")]
    public async Task<ActionResult<InventoryDto>> Restock(RestockRequest request, CancellationToken cancellationToken)
    {
        var inventory = await inventoryService.RestockAsync(request, cancellationToken);
        return Ok(inventory);
    }

    [HttpPost("adjust")]
    public async Task<ActionResult<InventoryDto>> Adjust(AdjustInventoryRequest request, CancellationToken cancellationToken)
    {
        var inventory = await inventoryService.AdjustAsync(request, cancellationToken);
        return Ok(inventory);
    }
}
