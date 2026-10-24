using Ecommerce.Application.Common;
using Ecommerce.Application.Inventory.Dtos;

namespace Ecommerce.Application.Inventory;

public interface IInventoryService
{
    /// <summary>Global, filterable, paginated stock view across the whole catalog, organized by
    /// product/category — complements the per-product restock/adjust panel on ProductsPage with a
    /// cross-product "what needs attention" view, which that per-product panel can't give without
    /// expanding every product one by one. Paginated at the PRODUCT level (page = N products, each
    /// with all of its variants), not the variant level.</summary>
    Task<PagedResult<InventoryProductOverviewDto>> GetOverviewAsync(
        InventoryOverviewFilter filter, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>One product's full stock detail (every active variant, all 5 quantity buckets) — backs
    /// the dedicated stock detail page a product row on /admin/inventory now links to.</summary>
    Task<InventoryProductOverviewDto> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);

    Task<InventoryDto> GetByVariantIdAsync(Guid variantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryTransactionDto>> GetTransactionsAsync(Guid variantId, CancellationToken cancellationToken = default);

    Task<InventoryDto> RestockAsync(RestockRequest request, CancellationToken cancellationToken = default);

    Task<InventoryDto> AdjustAsync(AdjustInventoryRequest request, CancellationToken cancellationToken = default);

    /// <summary>Moves stock from Available to Reserved. Throws if insufficient stock is available (never oversells).</summary>
    Task ReserveAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default);

    /// <summary>Moves stock from Reserved back to Available (e.g. order cancelled).</summary>
    Task ReleaseAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default);

    /// <summary>Moves stock from Reserved to Sold (order confirmed/shipped).</summary>
    Task RecordSaleAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default);

    /// <summary>Moves stock from Sold to Returned, or to Damaged when the return reason is a damaged item.</summary>
    Task RecordReturnAsync(Guid variantId, int quantity, bool isDamaged = false, CancellationToken cancellationToken = default);

    /// <summary>Moves stock from Reserved to Damaged — a delivery-failed order later found damaged when it comes back, never having been released.</summary>
    Task ReleaseToDamagedAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default);

    /// <summary>Moves stock from Available to Damaged — a refused order's stock (already released back to Available) later found damaged on inspection.</summary>
    Task MarkAvailableDamagedAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default);
}
