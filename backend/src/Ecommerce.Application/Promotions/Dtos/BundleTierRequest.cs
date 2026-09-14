namespace Ecommerce.Application.Promotions.Dtos;

public record BundleTierRequest(int BundleQuantity, decimal BundleTotalPrice, bool IncludesFreeShipping);
