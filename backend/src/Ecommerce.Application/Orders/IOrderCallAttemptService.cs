using Ecommerce.Application.Orders.Dtos;

namespace Ecommerce.Application.Orders;

public interface IOrderCallAttemptService
{
    Task<OrderDetailDto> RecordAsync(
        Guid orderId,
        RecordCallAttemptRequest request,
        Guid agentUserId,
        CancellationToken cancellationToken = default);
}
