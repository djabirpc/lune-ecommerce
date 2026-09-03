using System.Security.Cryptography;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Inventory;
using Ecommerce.Application.Orders;
using Ecommerce.Application.Orders.Dtos;
using Ecommerce.Application.Shipping;
using Ecommerce.Application.Shipping.Dtos;
using Ecommerce.Domain.Catalog;
using Ecommerce.Domain.Orders;
using Ecommerce.Domain.Promotions;
using Ecommerce.Domain.Shipping;
using Ecommerce.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Orders;

public class OrderService(
    AppDbContext dbContext,
    IInventoryService inventoryService,
    IShippingRateService shippingRateService,
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

        var baseShippingCost = await shippingRateService.GetPriceAsync(request.Wilaya, request.DeliveryType, cancellationToken);

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
            ShippingCost = baseShippingCost,
            UtmSource = request.MarketingAttribution?.UtmSource,
            UtmMedium = request.MarketingAttribution?.UtmMedium,
            UtmCampaign = request.MarketingAttribution?.UtmCampaign,
            UtmContent = request.MarketingAttribution?.UtmContent,
            UtmTerm = request.MarketingAttribution?.UtmTerm,
            Fbclid = request.MarketingAttribution?.Fbclid,
            Ttclid = request.MarketingAttribution?.Ttclid,
            Referrer = request.MarketingAttribution?.Referrer,
            LandingPage = request.MarketingAttribution?.LandingPage,
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

        var (discountTotal, shippingCost, appliedPromotions) = await CalculatePromotionsAsync(
            order.Items, variantsById, request.CouponCode, order.ShippingCost, cancellationToken);

        order.DiscountTotal = discountTotal;
        order.ShippingCost = shippingCost;
        order.Total = order.Subtotal - order.DiscountTotal + order.ShippingCost;
        foreach (var appliedPromotion in appliedPromotions)
        {
            order.AppliedPromotions.Add(appliedPromotion);
        }

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
            .Include(o => o.AppliedPromotions)
            .Include(o => o.Shipment).ThenInclude(s => s!.TrackingEvents)
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
            .Include(o => o.AppliedPromotions)
            .Include(o => o.Shipment).ThenInclude(s => s!.TrackingEvents)
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
            .Include(o => o.AppliedPromotions)
            .Include(o => o.Shipment).ThenInclude(s => s!.TrackingEvents)
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

    private async Task<(decimal DiscountTotal, decimal ShippingCost, List<OrderPromotion> AppliedPromotions)> CalculatePromotionsAsync(
        ICollection<OrderItem> items,
        Dictionary<Guid, ProductVariant> variantsById,
        string? couponCode,
        decimal shippingCost,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var productIds = items.Select(i => variantsById[i.ProductVariantId].ProductId).Distinct().ToList();
        var categoryIds = items.Select(i => variantsById[i.ProductVariantId].Product.CategoryId).Distinct().ToList();

        var candidates = await dbContext.Promotions
            .Include(p => p.Products)
            .Include(p => p.Categories)
            .Where(p => p.IsActive && p.Type != PromotionType.Coupon && p.StartsAtUtc <= now && p.EndsAtUtc >= now)
            .Where(p => (p.Products.Count == 0 && p.Categories.Count == 0)
                || p.Products.Any(pp => productIds.Contains(pp.ProductId))
                || p.Categories.Any(pc => categoryIds.Contains(pc.CategoryId)))
            .ToListAsync(cancellationToken);

        var appliedTotals = new Dictionary<Guid, (string Name, decimal Amount)>();
        var discountTotal = 0m;

        foreach (var item in items)
        {
            var variant = variantsById[item.ProductVariantId];
            var applicable = candidates
                .Where(p => p.Type != PromotionType.FreeShipping)
                .Where(p => IsScopedTo(p, variant.ProductId, variant.Product.CategoryId))
                .OrderByDescending(p => p.Priority)
                .FirstOrDefault();

            if (applicable is null)
            {
                continue;
            }

            var discount = ComputeItemDiscount(applicable, item);
            if (discount <= 0)
            {
                continue;
            }

            discountTotal += discount;
            Accumulate(appliedTotals, applicable.Id, applicable.Name, discount);
        }

        var freeShipping = candidates
            .Where(p => p.Type == PromotionType.FreeShipping)
            .Where(p => items.Any(i => IsScopedTo(p, variantsById[i.ProductVariantId].ProductId, variantsById[i.ProductVariantId].Product.CategoryId)))
            .OrderByDescending(p => p.Priority)
            .FirstOrDefault();

        var finalShippingCost = shippingCost;
        if (freeShipping is not null && shippingCost > 0)
        {
            discountTotal += shippingCost;
            Accumulate(appliedTotals, freeShipping.Id, freeShipping.Name, shippingCost);
            finalShippingCost = 0m;
        }

        if (!string.IsNullOrWhiteSpace(couponCode))
        {
            var coupon = await dbContext.Promotions
                .Include(p => p.Products)
                .Include(p => p.Categories)
                .FirstOrDefaultAsync(
                    p => p.Type == PromotionType.Coupon && p.CouponCode == couponCode
                        && p.IsActive && p.StartsAtUtc <= now && p.EndsAtUtc >= now,
                    cancellationToken)
                ?? throw new ValidationAppException("Code promo invalide ou expiré.");

            var couponBase = coupon.Products.Count == 0 && coupon.Categories.Count == 0
                ? items.Sum(i => i.LineTotal)
                : items
                    .Where(i => IsScopedTo(coupon, variantsById[i.ProductVariantId].ProductId, variantsById[i.ProductVariantId].Product.CategoryId))
                    .Sum(i => i.LineTotal);

            var couponDiscount = ComputeDiscount(coupon, couponBase);
            if (couponDiscount > 0)
            {
                discountTotal += couponDiscount;
                Accumulate(appliedTotals, coupon.Id, coupon.Name, couponDiscount);
            }
        }

        var appliedPromotions = appliedTotals
            .Select(kv => new OrderPromotion { PromotionId = kv.Key, PromotionName = kv.Value.Name, DiscountAmount = kv.Value.Amount })
            .ToList();

        return (discountTotal, finalShippingCost, appliedPromotions);
    }

    private static bool IsScopedTo(Promotion promotion, Guid productId, Guid categoryId) =>
        (promotion.Products.Count == 0 && promotion.Categories.Count == 0)
        || promotion.Products.Any(pp => pp.ProductId == productId)
        || promotion.Categories.Any(pc => pc.CategoryId == categoryId);

    private static decimal ComputeDiscount(Promotion promotion, decimal baseAmount) =>
        promotion.PercentageValue.HasValue
            ? Math.Round(baseAmount * promotion.PercentageValue.Value / 100m, 2)
            : promotion.FixedAmountValue.HasValue
                ? Math.Min(promotion.FixedAmountValue.Value, baseAmount)
                : 0m;

    /// <summary>
    /// BuyXGetY has its own discount shape (quantity bundles, not a percentage/fixed amount of the
    /// line), so it's dispatched separately rather than folded into ComputeDiscount.
    /// </summary>
    private static decimal ComputeItemDiscount(Promotion promotion, OrderItem item) =>
        promotion.Type == PromotionType.BuyXGetY
            ? ComputeBuyXGetYDiscount(promotion, item)
            : ComputeDiscount(promotion, item.LineTotal);

    /// <summary>
    /// "Buy X, Get Y" — every complete bundle of (BuyQuantity + GetQuantity) matching units in this
    /// line makes GetQuantity of them free. Evaluated per line item only (CLAUDE.md doesn't specify
    /// cross-line bundling, and the existing per-line promotion model doesn't support combining
    /// partial quantities across different variants) — e.g. "Buy 2 Get 1" needs 3+ units of the SAME
    /// variant in one line to trigger; buying 1 of variant A and 2 of variant B does not combine.
    /// A partial/incomplete bundle (e.g. 2 units on a "Buy 2 Get 1" promo) earns no discount.
    /// </summary>
    private static decimal ComputeBuyXGetYDiscount(Promotion promotion, OrderItem item)
    {
        if (promotion.BuyQuantity is not > 0 || promotion.GetQuantity is not > 0)
        {
            return 0m;
        }

        var bundleSize = promotion.BuyQuantity.Value + promotion.GetQuantity.Value;
        var completeBundles = item.Quantity / bundleSize;
        var freeUnits = completeBundles * promotion.GetQuantity.Value;

        return freeUnits * item.UnitPrice;
    }

    private static void Accumulate(Dictionary<Guid, (string Name, decimal Amount)> totals, Guid id, string name, decimal amount)
    {
        totals[id] = totals.TryGetValue(id, out var existing) ? (name, existing.Amount + amount) : (name, amount);
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
        order.DiscountTotal,
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
            : [],
        order.AppliedPromotions
            .Select(p => new OrderPromotionDto(p.Id, p.PromotionId, p.PromotionName, p.DiscountAmount))
            .ToList(),
        ToShipmentDto(order.Shipment),
        ToMarketingAttributionDto(order));

    private static MarketingAttributionDto? ToMarketingAttributionDto(Order order)
    {
        if (order.UtmSource is null && order.UtmMedium is null && order.UtmCampaign is null && order.UtmContent is null
            && order.UtmTerm is null && order.Fbclid is null && order.Ttclid is null && order.Referrer is null && order.LandingPage is null)
        {
            return null;
        }

        return new MarketingAttributionDto(
            order.UtmSource,
            order.UtmMedium,
            order.UtmCampaign,
            order.UtmContent,
            order.UtmTerm,
            order.Fbclid,
            order.Ttclid,
            order.Referrer,
            order.LandingPage);
    }

    private static ShipmentDto? ToShipmentDto(Shipment? shipment) => shipment is null ? null : new ShipmentDto(
        shipment.Id,
        shipment.OrderId,
        shipment.Carrier,
        shipment.ProviderShipmentId,
        shipment.TrackingNumber,
        shipment.ProviderStatus,
        shipment.NormalizedStatus,
        shipment.CreatedAtUtc,
        shipment.TrackingEvents
            .OrderBy(e => e.OccurredAtUtc)
            .Select(e => new ShipmentTrackingEventDto(e.Id, e.ProviderStatus, e.NormalizedStatus, e.Description, e.OccurredAtUtc))
            .ToList());
}
