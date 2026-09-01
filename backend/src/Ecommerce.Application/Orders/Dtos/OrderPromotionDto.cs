namespace Ecommerce.Application.Orders.Dtos;

public record OrderPromotionDto(Guid Id, Guid? PromotionId, string PromotionName, decimal DiscountAmount);
