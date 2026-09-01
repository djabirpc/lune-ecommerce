namespace Ecommerce.Application.Inventory.Dtos;

public record InventoryTransactionDto(
    Guid Id,
    Guid ProductVariantId,
    string Type,
    int Quantity,
    string? Reason,
    DateTime CreatedAtUtc);
