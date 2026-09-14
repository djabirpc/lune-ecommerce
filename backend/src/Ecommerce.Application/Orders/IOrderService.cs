using Ecommerce.Application.Common;
using Ecommerce.Application.Orders.Dtos;
using Ecommerce.Domain.Orders;

namespace Ecommerce.Application.Orders;

public interface IOrderService
{
    Task<OrderDetailDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);

    /// <summary>Staff-created order (e.g. a phone call): reuses the same validation/stock-reservation/
    /// promotion logic as a guest checkout, but starts the order already Confirmed (the call itself is
    /// the confirmation, so there's no PendingConfirmation step to work through), records who created
    /// it, and — unlike a guest checkout — can carry a phone-negotiated manual discount/free shipping.</summary>
    Task<OrderDetailDto> CreateAdminOrderAsync(CreateAdminOrderRequest request, Guid createdByUserId, CancellationToken cancellationToken = default);

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

    /// <summary>Counts of Returned orders grouped by their structured ReturnReason, most common first (admin dashboard).</summary>
    Task<IReadOnlyList<ReturnReasonSummaryDto>> GetReturnReasonSummaryAsync(CancellationToken cancellationToken = default);

    /// <summary>Adds a variant to the order (merging into an existing line if already present),
    /// reserves its stock, and recalculates totals. Only while the order hasn't shipped yet.</summary>
    Task<OrderDetailDto> AddItemAsync(Guid orderId, AddOrderItemRequest request, CancellationToken cancellationToken = default);

    /// <summary>Removes a line item, releases its reserved stock, and recalculates totals. Refuses to
    /// remove an order's last remaining item — cancel the order instead.</summary>
    Task<OrderDetailDto> RemoveItemAsync(Guid orderId, Guid orderItemId, CancellationToken cancellationToken = default);

    /// <summary>Sets or clears a phone-negotiated discount/free-shipping on an existing order and
    /// recalculates totals — the same mechanism available at creation (CreateAdminOrderRequest), made
    /// editable afterward.</summary>
    Task<OrderDetailDto> UpdateNegotiationAsync(Guid orderId, UpdateOrderNegotiationRequest request, CancellationToken cancellationToken = default);

    /// <summary>Free-text internal note, editable regardless of order status (unlike items/negotiation,
    /// there's no business-state risk to changing a note on an already-shipped order).</summary>
    Task<OrderDetailDto> UpdateNotesAsync(Guid orderId, UpdateOrderNotesRequest request, CancellationToken cancellationToken = default);
}
