using System.Security.Cryptography;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Inventory;
using Ecommerce.Application.Orders;
using Ecommerce.Application.Orders.Dtos;
using Ecommerce.Domain.Orders;
using Ecommerce.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Orders;

public class OrderService(
    AppDbContext dbContext,
    IInventoryService inventoryService,
    IValidator<CreateOrderRequest> createValidator,
    IValidator<ChangeOrderStatusRequest> changeStatusValidator) : IOrderService
{
    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
    {
        [OrderStatus.PendingConfirmation] = [OrderStatus.Confirmed, OrderStatus.CustomerUnreachable, OrderStatus.Cancelled],
        [OrderStatus.CustomerUnreachable] = [OrderStatus.Confirmed, OrderStatus.Cancelled],
        [OrderStatus.Confirmed] = [OrderStatus.Preparing, OrderStatus.Cancelled],
        [OrderStatus.Preparing] = [OrderStatus.ReadyToShip, OrderStatus.Cancelled],
        [OrderStatus.ReadyToShip] = [OrderStatus.Shipped, OrderStatus.Cancelled],
        [OrderStatus.Shipped] = [OrderStatus.OutForDelivery],
        [OrderStatus.OutForDelivery] = [OrderStatus.Delivered, OrderStatus.DeliveryFailed, OrderStatus.Refused],
        [OrderStatus.DeliveryFailed] = [OrderStatus.OutForDelivery, OrderStatus.Returned, OrderStatus.Cancelled],
        [OrderStatus.Refused] = [OrderStatus.Returned],
        [OrderStatus.Delivered] = [OrderStatus.Returned],
        [OrderStatus.Cancelled] = [],
        [OrderStatus.Returned] = [],
    };

    public async Task<OrderDetailDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var variantIds = request.Items.Select(i => i.ProductVariantId).ToList();
        var variants = await dbContext.ProductVariants
            .Include(v => v.Product)
            .Where(v => variantIds.Contains(v.Id))
            .ToListAsync(cancellationToken);

        var variantsById = variants.ToDictionary(v => v.Id);
        foreach (var variantId in variantIds)
        {
            if (!variantsById.TryGetValue(variantId, out var variant) || !variant.IsActive || !variant.Product.IsActive)
            {
                throw new NotFoundAppException("Une ou plusieurs variantes sont introuvables ou ne sont plus disponibles.");
            }
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var order = new Order
        {
            OrderNumber = await GenerateUniqueOrderNumberAsync(cancellationToken),
            Status = OrderStatus.PendingConfirmation,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            Wilaya = request.Wilaya,
            Commune = request.Commune,
            Address = request.Address,
            DeliveryType = request.DeliveryType,
            Notes = request.Notes,
            ShippingCost = 0m,
        };

        foreach (var itemRequest in request.Items)
        {
            var variant = variantsById[itemRequest.ProductVariantId];
            var unitPrice = variant.PriceOverride ?? variant.Product.Price;
            var lineTotal = unitPrice * itemRequest.Quantity;

            order.Items.Add(new OrderItem
            {
                ProductVariantId = variant.Id,
                ProductName = variant.Product.Name,
                Color = variant.Color,
                Size = variant.Size,
                Sku = variant.Sku,
                UnitPrice = unitPrice,
                Quantity = itemRequest.Quantity,
                LineTotal = lineTotal,
            });
        }

        order.Subtotal = order.Items.Sum(i => i.LineTotal);
        order.Total = order.Subtotal + order.ShippingCost;

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var item in order.Items)
        {
            await inventoryService.ReserveAsync(item.ProductVariantId, item.Quantity, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return ToDetailDto(order);
    }

    public async Task<OrderDetailDto> TrackAsync(string orderNumber, string phone, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders.AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, cancellationToken);

        return order is null || order.Phone != phone
            ? throw new NotFoundAppException("Commande introuvable.")
            : ToDetailDto(order);
    }

    public async Task<OrderDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders.AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .Include(o => o.CallAttempts)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken)
            ?? throw new NotFoundAppException("Commande introuvable.");

        return ToDetailDto(order, includeHistory: true);
    }

    public async Task<PagedResult<OrderSummaryDto>> GetPagedAsync(
        OrderStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = dbContext.Orders.AsNoTracking();

        if (status.HasValue)
        {
            query = query.Where(o => o.Status == status.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(o => o.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderSummaryDto(
                o.Id,
                o.OrderNumber,
                o.Status,
                o.FirstName + " " + o.LastName,
                o.Phone,
                o.Wilaya,
                o.Total,
                o.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<OrderSummaryDto>(items, page, pageSize, totalCount);
    }

    public async Task<OrderDetailDto> ChangeStatusAsync(
        Guid orderId,
        ChangeOrderStatusRequest request,
        Guid? changedByUserId,
        CancellationToken cancellationToken = default)
    {
        await changeStatusValidator.ValidateAndThrowAsync(request, cancellationToken);

        var order = await dbContext.Orders
            .Include(o => o.Items)
            .Include(o => o.StatusHistory)
            .Include(o => o.CallAttempts)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new NotFoundAppException("Commande introuvable.");

        if (!AllowedTransitions.TryGetValue(order.Status, out var allowed) || !allowed.Contains(request.NewStatus))
        {
            throw new ConflictAppException($"Impossible de passer du statut {order.Status} au statut {request.NewStatus}.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        switch (request.NewStatus)
        {
            case OrderStatus.Cancelled or OrderStatus.Refused:
                foreach (var item in order.Items)
                {
                    await inventoryService.ReleaseAsync(item.ProductVariantId, item.Quantity, cancellationToken);
                }
                break;

            case OrderStatus.Delivered:
                foreach (var item in order.Items)
                {
                    await inventoryService.RecordSaleAsync(item.ProductVariantId, item.Quantity, cancellationToken);
                }
                order.PaymentStatus = PaymentStatus.Collected;
                break;

            case OrderStatus.Returned:
                foreach (var item in order.Items)
                {
                    await inventoryService.RecordReturnAsync(item.ProductVariantId, item.Quantity, cancellationToken);
                }
                break;
        }

        var oldStatus = order.Status;
        order.Status = request.NewStatus;
        order.UpdatedAtUtc = DateTime.UtcNow;

        dbContext.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            OldStatus = oldStatus,
            NewStatus = request.NewStatus,
            ChangedByUserId = changedByUserId,
            Reason = request.Reason,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ToDetailDto(order, includeHistory: true);
    }

    private async Task<string> GenerateUniqueOrderNumberAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var candidate = $"LUNA-{DateTime.UtcNow:yyMMdd}-{RandomNumberGenerator.GetInt32(1000, 9999)}";
            if (!await dbContext.Orders.AnyAsync(o => o.OrderNumber == candidate, cancellationToken))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("Impossible de générer un numéro de commande unique après plusieurs tentatives.");
    }

    private static OrderDetailDto ToDetailDto(Order order, bool includeHistory = false) => new(
        order.Id,
        order.OrderNumber,
        order.Status,
        order.FirstName,
        order.LastName,
        order.Phone,
        order.Wilaya,
        order.Commune,
        order.Address,
        order.DeliveryType,
        order.Notes,
        order.PaymentMethod,
        order.PaymentStatus,
        order.Subtotal,
        order.ShippingCost,
        order.Total,
        order.CreatedAtUtc,
        order.Items
            .Select(i => new OrderItemDto(i.Id, i.ProductVariantId, i.ProductName, i.Color, i.Size, i.Sku, i.UnitPrice, i.Quantity, i.LineTotal))
            .ToList(),
        includeHistory
            ? order.StatusHistory
                .OrderBy(h => h.CreatedAtUtc)
                .Select(h => new OrderStatusHistoryDto(h.Id, h.OldStatus, h.NewStatus, h.Reason, h.CreatedAtUtc))
                .ToList()
            : [],
        includeHistory
            ? order.CallAttempts
                .OrderBy(a => a.CalledAtUtc)
                .Select(a => new OrderCallAttemptDto(a.Id, a.AttemptNumber, a.Result, a.Notes, a.CalledAtUtc, a.NextCallAtUtc))
                .ToList()
            : []);
}
