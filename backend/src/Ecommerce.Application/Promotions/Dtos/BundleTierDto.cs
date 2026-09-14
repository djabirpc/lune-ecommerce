namespace Ecommerce.Application.Promotions.Dtos;

public record BundleTierDto(Guid Id, int BundleQuantity, decimal BundleTotalPrice, bool IncludesFreeShipping);
