namespace Ecommerce.Application.Orders.Dtos;

/// <summary>Adds a variant to an existing order (or increases its quantity if already present) —
/// e.g. an agent on a call: "I'll also take this one." Only allowed while the order hasn't shipped
/// yet (OrderService.EditableStatuses).</summary>
public record AddOrderItemRequest(Guid ProductVariantId, int Quantity);
