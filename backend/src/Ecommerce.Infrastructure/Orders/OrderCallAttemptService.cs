using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Orders;
using Ecommerce.Application.Orders.Dtos;
using Ecommerce.Domain.Orders;
using Ecommerce.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Orders;

public class OrderCallAttemptService(
    AppDbContext dbContext,
    IOrderService orderService,
    IValidator<RecordCallAttemptRequest> validator) : IOrderCallAttemptService
{
    public async Task<OrderDetailDto> RecordAsync(
        Guid orderId,
        RecordCallAttemptRequest request,
        Guid agentUserId,
        CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var order = await dbContext.Orders
            .Include(o => o.CallAttempts)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new NotFoundAppException("Commande introuvable.");

        if (order.Status is not (OrderStatus.PendingConfirmation or OrderStatus.CustomerUnreachable))
        {
            throw new ConflictAppException("Cette commande n'est pas en attente de confirmation.");
        }

        dbContext.OrderCallAttempts.Add(new OrderCallAttempt
        {
            OrderId = order.Id,
            AgentUserId = agentUserId,
            AttemptNumber = order.CallAttempts.Count + 1,
            Result = request.Result,
            Notes = request.Notes,
            NextCallAtUtc = request.NextCallAt,
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return request.Result switch
        {
            CallAttemptResult.Confirmed => await orderService.ChangeStatusAsync(
                orderId,
                new ChangeOrderStatusRequest(OrderStatus.Confirmed, "Confirmée suite à un appel client."),
                agentUserId,
                cancellationToken),
            CallAttemptResult.Cancelled => await orderService.ChangeStatusAsync(
                orderId,
                new ChangeOrderStatusRequest(OrderStatus.Cancelled, "Annulée suite à un appel client."),
                agentUserId,
                cancellationToken),
            // Only the *first* NoAnswer (while still PendingConfirmation) actually transitions the order —
            // a later NoAnswer recorded while the order is already CustomerUnreachable just appends another
            // attempt to the log without trying (and failing, since CustomerUnreachable → CustomerUnreachable
            // isn't a valid transition) to re-apply the same status. This is what lets an agent record "still
            // unreachable" on a 2nd/3rd call without any special-casing on their end.
            CallAttemptResult.NoAnswer when order.Status == OrderStatus.PendingConfirmation => await orderService.ChangeStatusAsync(
                orderId,
                new ChangeOrderStatusRequest(OrderStatus.CustomerUnreachable, "Injoignable après appel (sans réponse)."),
                agentUserId,
                cancellationToken),
            _ => await orderService.GetByIdAsync(orderId, cancellationToken),
        };
    }
}
