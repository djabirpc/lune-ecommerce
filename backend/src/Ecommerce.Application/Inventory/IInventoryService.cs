using Ecommerce.Application.Inventory.Dtos;

namespace Ecommerce.Application.Inventory;

public interface IInventoryService
{
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

    /// <summary>Moves stock from Sold to Returned (customer return).</summary>
    Task RecordReturnAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default);
}
