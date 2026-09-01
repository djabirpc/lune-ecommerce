namespace Ecommerce.Application.Inventory.Dtos;

public record InventoryDto(
    Guid ProductVariantId,
    string Sku,
    int AvailableQuantity,
    int ReservedQuantity,
    int SoldQuantity,
    int ReturnedQuantity,
    int DamagedQuantity);
