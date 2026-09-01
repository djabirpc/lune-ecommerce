using Ecommerce.Application.Common;
using Ecommerce.Application.Orders.Dtos;
using Ecommerce.Domain.Orders;

namespace Ecommerce.Application.Orders;

public interface IOrderService
{
    Task<OrderDetailDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>Guest order tracking: requires the phone number to match, to avoid order-number enumeration.</summary>
    Task<OrderDetailDto> TrackAsync(string orderNumber, string phone, CancellationToken cancellationToken = default);

    Task<OrderDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<OrderSummaryDto>> GetPagedAsync(
        OrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<OrderDetailDto> ChangeStatusAsync(
        Guid orderId,
        ChangeOrderStatusRequest request,
        Guid? changedByUserId,
        CancellationToken cancellationToken = default);
}
