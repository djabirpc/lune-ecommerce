using Ecommerce.Domain.Orders;

namespace Ecommerce.Application.Orders.Dtos;

public record ReturnReasonSummaryDto(OrderReturnReason Reason, int Count);
