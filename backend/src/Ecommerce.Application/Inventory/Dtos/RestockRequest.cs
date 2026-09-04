namespace Ecommerce.Application.Inventory.Dtos;

public record RestockRequest(
    Guid ProductVariantId,
    int Quantity,
    string? Reason,
    Guid? SupplierId = null,
    decimal? UnitCost = null);
