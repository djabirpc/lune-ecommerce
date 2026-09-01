namespace Ecommerce.Application.Inventory.Dtos;

public record AdjustInventoryRequest(Guid ProductVariantId, int QuantityDelta, string Reason);
