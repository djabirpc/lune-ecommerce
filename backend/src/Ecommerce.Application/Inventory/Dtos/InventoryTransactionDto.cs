namespace Ecommerce.Application.Inventory.Dtos;

public record InventoryTransactionDto(
    Guid Id,
    Guid ProductVariantId,
    string Type,
    int Quantity,
    string? Reason,
    Guid? SupplierId,
    string? SupplierName,
    decimal? UnitCost,
    DateTime CreatedAtUtc);
