using Ecommerce.Application.Common;
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
    [HttpGet]
    public async Task<ActionResult<PagedResult<InventoryProductOverviewDto>>> GetOverview(
        [FromQuery] string? search,
        [FromQuery] Guid? categoryId,
        [FromQuery] InventoryStockStatus stockStatus = InventoryStockStatus.All,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var filter = new InventoryOverviewFilter(search, categoryId, stockStatus);
        var result = await inventoryService.GetOverviewAsync(filter, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("by-product/{productId:guid}")]
    public async Task<ActionResult<InventoryProductOverviewDto>> GetByProduct(Guid productId, CancellationToken cancellationToken)
    {
        return Ok(await inventoryService.GetByProductIdAsync(productId, cancellationToken));
    }

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
