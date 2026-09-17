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
    IValidator<CreateAdminOrderRequest> createAdminValidator,
    IValidator<ChangeOrderStatusRequest> changeStatusValidator,
    IValidator<AddOrderItemRequest> addItemValidator,
    IValidator<UpdateOrderNegotiationRequest> updateNegotiationValidator,
    IValidator<UpdateOrderNotesRequest> updateNotesValidator) : IOrderService
{
    // Mirrors CLAUDE.md section 12's workflow: once an order is Shipped (physically handed to the
    // carrier), its contents/pricing are frozen — only PendingConfirmation through ReadyToShip allow
    // adding/removing items or changing the negotiated discount.
    private static readonly HashSet<OrderStatus> EditableStatuses =
    [
        OrderStatus.PendingConfirmation,
        OrderStatus.Confirmed,
        OrderStatus.CustomerUnreachable,
        OrderStatus.ReadyToShip,
    ];

    private static readonly Dictionary<OrderStatus, OrderStatus[]> AllowedTransitions = new()
    {
        [OrderStatus.PendingConfirmation] = [OrderStatus.Confirmed, OrderStatus.CustomerUnreachable, OrderStatus.Cancelled],
        // CustomerUnreachable -> CustomerUnreachable is a deliberate self-transition (not a no-op
        // guard elsewhere) so an agent can repeatedly mark "still unreachable" after each follow-up
        // call — each click appends a fresh OrderStatusHistory row (with its own optional reason),
        // which doubles as the call-attempt log per the user's request to simplify this workflow
        // instead of using the separate OrderCallAttempt form.
        [OrderStatus.CustomerUnreachable] = [OrderStatus.Confirmed, OrderStatus.Cancelled, OrderStatus.CustomerUnreachable],
        // Confirmed -> ReadyToShip directly: the former separate "Preparing" step was merged into
        // this one, per direct request — a confirmed order goes straight to "ready to create a
        // shipment" in one agent click instead of two.
        [OrderStatus.Confirmed] = [OrderStatus.ReadyToShip, OrderStatus.Cancelled],
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
        return await CreateOrderCoreAsync(request, OrderStatus.PendingConfirmation, null, null, false, cancellationToken);
    }

    public async Task<OrderDetailDto> CreateAdminOrderAsync(CreateAdminOrderRequest request, Guid createdByUserId, CancellationToken cancellationToken = default)
    {
        await createAdminValidator.ValidateAndThrowAsync(request, cancellationToken);

        var coreRequest = new CreateOrderRequest(
            request.FirstName,
            request.LastName,
            request.Phone,
            request.Wilaya,
            request.Commune,
            request.Address,
            request.DeliveryType,
            request.Notes,
            request.Items,
            request.CouponCode);

        return await CreateOrderCoreAsync(coreRequest, OrderStatus.Confirmed, createdByUserId, request.ManualDiscountAmount, request.FreeShipping, cancellationToken);
    }

    private async Task<OrderDetailDto> CreateOrderCoreAsync(
        CreateOrderRequest request,
        OrderStatus initialStatus,
        Guid? createdByUserId,
        decimal? manualDiscountAmount,
        bool negotiatedFreeShipping,
        CancellationToken cancellationToken)
    {
        var variantIds = request.Items.Select(i => i.ProductVariantId).ToList();
        var variants = await dbContext.ProductVariants
            .Include(v => v.Product).ThenInclude(p => p.Images)
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
            Status = initialStatus,
            CreatedByUserId = createdByUserId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            Wilaya = request.Wilaya,
            Commune = request.Commune,
            Address = request.Address,
            DeliveryType = request.DeliveryType,
            Notes = request.Notes,
            CouponCode = request.CouponCode,
            ManualDiscountAmount = manualDiscountAmount is > 0 ? manualDiscountAmount : null,
            NegotiatedFreeShipping = negotiatedFreeShipping,
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

            order.Items.Add(new OrderItem
            {
                ProductVariantId = variant.Id,
                ProductName = variant.Product.Name,
                ProductSlug = variant.Product.Slug,
                ImageUrl = variant.Product.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault(),
                Color = variant.Color,
                Size = variant.Size,
                Sku = variant.Sku,
                UnitPrice = unitPrice,
                Quantity = itemRequest.Quantity,
                LineTotal = unitPrice * itemRequest.Quantity,
            });
        }

        await RecalculateTotalsAsync(order, cancellationToken);

        dbContext.Orders.Add(order);
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var item in order.Items)
        {
            await inventoryService.ReserveAsync(item.ProductVariantId, item.Quantity, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return ToDetailDto(order);
    }

    public async Task<OrderDetailDto> AddItemAsync(Guid orderId, AddOrderItemRequest request, CancellationToken cancellationToken = default)
    {
        await addItemValidator.ValidateAndThrowAsync(request, cancellationToken);

        var order = await dbContext.Orders
            .Include(o => o.Items)
            .Include(o => o.AppliedPromotions)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new NotFoundAppException("Commande introuvable.");

        EnsureEditable(order);

        var variant = await dbContext.ProductVariants
            .Include(v => v.Product).ThenInclude(p => p.Images)
            .FirstOrDefaultAsync(v => v.Id == request.ProductVariantId, cancellationToken)
            ?? throw new NotFoundAppException("Variante introuvable.");

        if (!variant.IsActive || !variant.Product.IsActive)
        {
            throw new NotFoundAppException("Cette variante n'est plus disponible.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Order changes are saved BEFORE reserving stock (mirrors CreateOrderCoreAsync), so a failed
        // reservation still rolls back the order mutation (both share this one ambient transaction).
        var existingItem = order.Items.FirstOrDefault(i => i.ProductVariantId == request.ProductVariantId);
        if (existingItem is not null)
        {
            existingItem.Quantity += request.Quantity;
            existingItem.LineTotal = existingItem.UnitPrice * existingItem.Quantity;
        }
        else
        {
            var unitPrice = variant.PriceOverride ?? variant.Product.Price;
            var newItem = new OrderItem
            {
                OrderId = order.Id,
                ProductVariantId = variant.Id,
                ProductName = variant.Product.Name,
                ProductSlug = variant.Product.Slug,
                ImageUrl = variant.Product.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault(),
                Color = variant.Color,
                Size = variant.Size,
                Sku = variant.Sku,
                UnitPrice = unitPrice,
                Quantity = request.Quantity,
                LineTotal = unitPrice * request.Quantity,
            };
            // Explicit dbContext.OrderItems.Add(), not order.Items.Add() — for a child discovered by
            // DetectChanges() via graph traversal from an ALREADY-TRACKED parent (as opposed to the
            // whole graph being tracked at once via dbContext.Orders.Add(newOrder) at creation time),
            // EF's Added-vs-Modified heuristic keys off whether the PK already has a non-default
            // value. Entity.Id is client-generated (Guid.NewGuid() at construction, not DB-generated),
            // so it's already "set" by the time DetectChanges sees it — EF assumed it must be an
            // existing row and tried to UPDATE a row that was never inserted, throwing
            // DbUpdateConcurrencyException ("expected 1 row, affected 0"). Adding directly to the
            // DbSet sidesteps the ambiguity by stating the state explicitly (same fix applied to the
            // OrderPromotion rebuild below). Setting OrderId explicitly makes EF's relationship fixup
            // add this same instance to order.Items automatically — do NOT also call
            // order.Items.Add(newItem) here, that double-counts it (fixup already did it).
            dbContext.OrderItems.Add(newItem);
        }

        order.UpdatedAtUtc = DateTime.UtcNow;
        await RecalculateTotalsAsync(order, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await inventoryService.ReserveAsync(variant.Id, request.Quantity, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return await GetByIdAsync(orderId, cancellationToken);
    }

    public async Task<OrderDetailDto> RemoveItemAsync(Guid orderId, Guid orderItemId, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders
            .Include(o => o.Items)
            .Include(o => o.AppliedPromotions)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new NotFoundAppException("Commande introuvable.");

        EnsureEditable(order);

        var item = order.Items.FirstOrDefault(i => i.Id == orderItemId)
            ?? throw new NotFoundAppException("Article introuvable sur cette commande.");

        if (order.Items.Count <= 1)
        {
            throw new ValidationAppException("Impossible de retirer le dernier article d'une commande — annulez la commande à la place.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Same save-before-release ordering as AddItemAsync — see its comment for why.
        order.Items.Remove(item);
        order.UpdatedAtUtc = DateTime.UtcNow;
        await RecalculateTotalsAsync(order, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await inventoryService.ReleaseAsync(item.ProductVariantId, item.Quantity, cancellationToken);

        await transaction.CommitAsync(cancellationToken);

        return await GetByIdAsync(orderId, cancellationToken);
    }

    public async Task<OrderDetailDto> UpdateNegotiationAsync(Guid orderId, UpdateOrderNegotiationRequest request, CancellationToken cancellationToken = default)
    {
        await updateNegotiationValidator.ValidateAndThrowAsync(request, cancellationToken);

        var order = await dbContext.Orders
            .Include(o => o.Items)
            .Include(o => o.AppliedPromotions)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new NotFoundAppException("Commande introuvable.");

        EnsureEditable(order);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        order.ManualDiscountAmount = request.ManualDiscountAmount is > 0 ? request.ManualDiscountAmount : null;
        order.NegotiatedFreeShipping = request.FreeShipping;
        order.UpdatedAtUtc = DateTime.UtcNow;

        await RecalculateTotalsAsync(order, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return await GetByIdAsync(orderId, cancellationToken);
    }

    public async Task<OrderDetailDto> UpdateNotesAsync(Guid orderId, UpdateOrderNotesRequest request, CancellationToken cancellationToken = default)
    {
        await updateNotesValidator.ValidateAndThrowAsync(request, cancellationToken);

        var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new NotFoundAppException("Commande introuvable.");

        order.Notes = request.Notes;
        order.UpdatedAtUtc = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(orderId, cancellationToken);
    }

    private static void EnsureEditable(Order order)
    {
        if (!EditableStatuses.Contains(order.Status))
        {
            throw new ConflictAppException($"Impossible de modifier les articles d'une commande au statut {order.Status}.");
        }
    }

    /// <summary>
    /// Recomputes Subtotal/DiscountTotal/ShippingCost/Total and rebuilds AppliedPromotions from
    /// scratch — shared by order creation and every later edit (item add/remove, negotiation change)
    /// so both paths always apply automatic promotions + the stored coupon + the stored manual
    /// discount/free-shipping the exact same way. Assumes order.Items reflects the desired final
    /// state and order.AppliedPromotions is a loaded, tracked collection (callers must .Include it).
    /// </summary>
    private async Task RecalculateTotalsAsync(Order order, CancellationToken cancellationToken)
    {
        var baseShippingCost = await shippingRateService.GetPriceAsync(order.Wilaya, order.DeliveryType, cancellationToken);

        var variantIds = order.Items.Select(i => i.ProductVariantId).Distinct().ToList();
        var variants = await dbContext.ProductVariants
            .Include(v => v.Product)
            .Where(v => variantIds.Contains(v.Id))
            .ToListAsync(cancellationToken);
        var variantsById = variants.ToDictionary(v => v.Id);

        order.Subtotal = order.Items.Sum(i => i.LineTotal);

        var (discountTotal, shippingCost, appliedPromotions) = await CalculatePromotionsAsync(
            order.Items, variantsById, order.CouponCode, baseShippingCost, cancellationToken);

        // Phone-negotiated overrides, stacked on top of automatic promotions/coupon just like a
        // coupon stacks on top of an automatic promotion — capped at the remaining discountable
        // subtotal so a mistyped amount (or a removed item shrinking the subtotal) can never push
        // the total negative.
        if (order.ManualDiscountAmount is > 0)
        {
            var remainingDiscountableSubtotal = Math.Max(order.Subtotal - discountTotal, 0m);
            var appliedManualDiscount = Math.Min(order.ManualDiscountAmount.Value, remainingDiscountableSubtotal);
            if (appliedManualDiscount > 0)
            {
                discountTotal += appliedManualDiscount;
                appliedPromotions.Add(new OrderPromotion
                {
                    PromotionId = null,
                    PromotionName = "Remise négociée (téléphone)",
                    DiscountAmount = appliedManualDiscount,
                });
            }
        }

        if (order.NegotiatedFreeShipping && shippingCost > 0)
        {
            discountTotal += shippingCost;
            appliedPromotions.Add(new OrderPromotion
            {
                PromotionId = null,
                PromotionName = "Livraison offerte (négociée)",
                DiscountAmount = shippingCost,
            });
            shippingCost = 0m;
        }

        order.DiscountTotal = discountTotal;
        order.ShippingCost = shippingCost;
        order.Total = order.Subtotal - order.DiscountTotal + order.ShippingCost;

        // Explicit DbSet remove/add rather than order.AppliedPromotions.Clear()/Add() — for an
        // order that's already tracked (every edit path; creation never reaches here with a tracked
        // order), the collection-navigation form left EF unable to tell a genuinely-new OrderPromotion
        // apart from a re-attached one and tried to UPDATE it instead of INSERT, throwing
        // DbUpdateConcurrencyException ("expected 1 row, affected 0"). Being explicit about
        // Remove/Add (and the FK) sidesteps that ambiguity entirely.
        if (order.AppliedPromotions.Count > 0)
        {
            dbContext.OrderPromotions.RemoveRange(order.AppliedPromotions);
            order.AppliedPromotions.Clear();
        }

        foreach (var appliedPromotion in appliedPromotions)
        {
            appliedPromotion.OrderId = order.Id;
            dbContext.OrderPromotions.Add(appliedPromotion);
        }
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

        var userNames = await ResolveUserNamesAsync(HistoryActorIds(order), cancellationToken);

        return ToDetailDto(order, includeHistory: true, userNames);
    }

    /// <summary>
    /// Acting-user ids referenced by an order's status/call history — resolved to display names once
    /// per request (see ResolveUserNamesAsync) rather than N+1 queried, since the admin Order/Confirmation
    /// Summary panels attribute each activity-feed entry to the staff member who made it.
    /// </summary>
    private static IEnumerable<Guid> HistoryActorIds(Order order) =>
        order.StatusHistory.Where(h => h.ChangedByUserId.HasValue).Select(h => h.ChangedByUserId!.Value)
            .Concat(order.CallAttempts.Select(a => a.AgentUserId));

    private async Task<Dictionary<Guid, string>> ResolveUserNamesAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        return await dbContext.Users
            .Where(u => ids.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}".Trim(), cancellationToken);
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
                // Which inventory bucket the stock is currently sitting in depends on where the order
                // came from: Delivered means it was actually sold (Sold bucket); Refused already
                // released its reservation back to Available when it *became* Refused (see the
                // Cancelled/Refused case above); DeliveryFailed never touched inventory at all, so the
                // stock is still sitting in Reserved. Treating every "Returned" the same way (as if it
                // had been Sold) was the original bug — see PROJECT_CONTEXT.md Known Issues.
                var isDamaged = request.ReturnReason == OrderReturnReason.Damaged;
                foreach (var item in order.Items)
                {
                    switch (order.Status)
                    {
                        case OrderStatus.Delivered:
                            await inventoryService.RecordReturnAsync(item.ProductVariantId, item.Quantity, isDamaged, cancellationToken);
                            break;
                        case OrderStatus.DeliveryFailed:
                            if (isDamaged)
                            {
                                await inventoryService.ReleaseToDamagedAsync(item.ProductVariantId, item.Quantity, cancellationToken);
                            }
                            else
                            {
                                await inventoryService.ReleaseAsync(item.ProductVariantId, item.Quantity, cancellationToken);
                            }
                            break;
                        case OrderStatus.Refused:
                            if (isDamaged)
                            {
                                await inventoryService.MarkAvailableDamagedAsync(item.ProductVariantId, item.Quantity, cancellationToken);
                            }
                            break;
                    }
                }
                order.ReturnReason = request.ReturnReason;
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

        var userNames = await ResolveUserNamesAsync(HistoryActorIds(order), cancellationToken);

        return ToDetailDto(order, includeHistory: true, userNames);
    }

    public async Task<IReadOnlyList<ReturnReasonSummaryDto>> GetReturnReasonSummaryAsync(CancellationToken cancellationToken = default)
    {
        // GroupBy aggregates can't project straight into a record constructor (EF Core translation
        // limitation, see the Marketing namespace gotcha in PROJECT_CONTEXT.md) — project into an
        // anonymous type first, materialize, then map to the DTO record client-side.
        var grouped = await dbContext.Orders
            .Where(o => o.Status == OrderStatus.Returned && o.ReturnReason != null)
            .GroupBy(o => o.ReturnReason)
            .Select(g => new { Reason = g.Key!.Value, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return grouped
            .OrderByDescending(g => g.Count)
            .Select(g => new ReturnReasonSummaryDto(g.Reason, g.Count))
            .ToList();
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
            .Include(p => p.BundleTiers)
            .Where(p => p.IsActive && p.Type != PromotionType.Coupon && p.StartsAtUtc <= now && p.EndsAtUtc >= now)
            .Where(p => (p.Products.Count == 0 && p.Categories.Count == 0)
                || p.Products.Any(pp => productIds.Contains(pp.ProductId))
                || p.Categories.Any(pc => categoryIds.Contains(pc.CategoryId)))
            .ToListAsync(cancellationToken);

        var appliedTotals = new Dictionary<Guid, (string Name, decimal Amount)>();
        var discountTotal = 0m;
        Promotion? bundleFreeShippingPromotion = null;

        foreach (var item in items)
        {
            var variant = variantsById[item.ProductVariantId];
            var scoped = candidates
                .Where(p => p.Type != PromotionType.FreeShipping)
                .Where(p => IsScopedTo(p, variant.ProductId, variant.Product.CategoryId))
                .ToList();

            if (scoped.Count == 0)
            {
                continue;
            }

            // Best discount for the actual quantity — not simply highest Priority — so multiple
            // promotions, and multiple BundlePrice tiers within one promotion (e.g. "2 for 1500" and
            // "4 for 3000"), can all coexist on the same product; the customer's cart automatically
            // gets whichever one is most advantageous for their actual quantity. Priority only breaks
            // ties when two candidates give the exact same discount.
            Promotion? bestPromotion = null;
            PromotionBundleTier? bestTier = null;
            var bestDiscount = 0m;

            foreach (var promotion in scoped)
            {
                var (discount, tier) = promotion.Type == PromotionType.BundlePrice
                    ? ComputeBestBundleTierDiscount(promotion, item)
                    : (ComputeItemDiscount(promotion, item), null);

                if (discount <= 0)
                {
                    continue;
                }

                if (bestPromotion is null || discount > bestDiscount || (discount == bestDiscount && promotion.Priority > bestPromotion.Priority))
                {
                    bestDiscount = discount;
                    bestPromotion = promotion;
                    bestTier = tier;
                }
            }

            if (bestPromotion is null)
            {
                continue;
            }

            discountTotal += bestDiscount;
            Accumulate(appliedTotals, bestPromotion.Id, bestPromotion.Name, bestDiscount);

            if (bestTier is { IncludesFreeShipping: true })
            {
                bundleFreeShippingPromotion ??= bestPromotion;
            }
        }

        var freeShipping = candidates
            .Where(p => p.Type == PromotionType.FreeShipping)
            .Where(p => IsFreeShippingEligible(p, items, variantsById))
            .OrderByDescending(p => p.Priority)
            .FirstOrDefault();

        // A dedicated FreeShipping promotion (if eligible) takes precedence over a bundle tier that
        // merely includes free shipping — both waive the same shipping cost, so there's nothing to
        // stack, just pick whichever is already resolved as "the" free shipping grant.
        var freeShippingGrant = freeShipping is not null
            ? (freeShipping.Id, freeShipping.Name)
            : bundleFreeShippingPromotion is not null
                ? (bundleFreeShippingPromotion.Id, bundleFreeShippingPromotion.Name)
                : ((Guid Id, string Name)?)null;

        var finalShippingCost = shippingCost;
        if (freeShippingGrant is not null && shippingCost > 0)
        {
            discountTotal += shippingCost;
            Accumulate(appliedTotals, freeShippingGrant.Value.Id, freeShippingGrant.Value.Name, shippingCost);
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

    /// <summary>
    /// A FreeShipping promotion is eligible once at least one scoped item is in the cart, and (if
    /// MinQuantity is set) the total quantity of scoped items reaches that threshold — e.g. pairing a
    /// "2 for 1500 DA" BundlePrice offer with a separate FreeShipping promotion (MinQuantity: 4) on the
    /// same product to reward "4+ items also ships free" without a bespoke tiered-bundle model.
    /// </summary>
    private static bool IsFreeShippingEligible(Promotion promotion, ICollection<OrderItem> items, Dictionary<Guid, ProductVariant> variantsById)
    {
        var scopedQuantity = items
            .Where(i => IsScopedTo(promotion, variantsById[i.ProductVariantId].ProductId, variantsById[i.ProductVariantId].Product.CategoryId))
            .Sum(i => i.Quantity);

        if (scopedQuantity == 0)
        {
            return false;
        }

        return promotion.MinQuantity is not > 0 || scopedQuantity >= promotion.MinQuantity.Value;
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
    /// line), so it's dispatched separately rather than folded into ComputeDiscount. BundlePrice is
    /// NOT handled here — it has multiple tiers per promotion, so CalculatePromotionsAsync's loop
    /// dispatches it directly to ComputeBestBundleTierDiscount instead.
    /// </summary>
    private static decimal ComputeItemDiscount(Promotion promotion, OrderItem item) =>
        promotion.Type switch
        {
            PromotionType.BuyXGetY => ComputeBuyXGetYDiscount(promotion, item),
            _ => ComputeDiscount(promotion, item.LineTotal),
        };

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

    /// <summary>
    /// Best tier (by discount for the actual quantity) among a BundlePrice promotion's tiers — lets
    /// several tiers on one promotion coexist (e.g. "2 for 1500" and "4 for 3000"); the customer
    /// automatically gets whichever tier is most advantageous for their real quantity, and the
    /// returned tier is what CalculatePromotionsAsync checks for IncludesFreeShipping.
    /// </summary>
    private static (decimal Discount, PromotionBundleTier? Tier) ComputeBestBundleTierDiscount(Promotion promotion, OrderItem item)
    {
        PromotionBundleTier? bestTier = null;
        var bestDiscount = 0m;

        foreach (var tier in promotion.BundleTiers)
        {
            var discount = ComputeBundleTierDiscount(tier, item);
            if (discount > bestDiscount)
            {
                bestDiscount = discount;
                bestTier = tier;
            }
        }

        return (bestDiscount, bestTier);
    }

    /// <summary>
    /// "N for a fixed total price" (e.g. "2 for 1500 DA" on a 1000 DA item). Every complete bundle of
    /// BundleQuantity matching units in this line is charged BundleTotalPrice instead of
    /// BundleQuantity * UnitPrice; any remainder units (an incomplete bundle) stay at full price.
    /// Same per-line-only evaluation as BuyXGetY (see ComputeBuyXGetYDiscount) — e.g. "2 for 1500"
    /// needs 2+ units of the SAME variant in one line. Floors at 0 so a misconfigured bundle price
    /// higher than the regular price can never produce a negative "discount".
    /// </summary>
    private static decimal ComputeBundleTierDiscount(PromotionBundleTier tier, OrderItem item)
    {
        if (tier.BundleQuantity < 2 || tier.BundleTotalPrice <= 0)
        {
            return 0m;
        }

        var completeBundles = item.Quantity / tier.BundleQuantity;
        if (completeBundles == 0)
        {
            return 0m;
        }

        var regularPriceForBundledUnits = completeBundles * tier.BundleQuantity * item.UnitPrice;
        var bundlePriceForBundledUnits = completeBundles * tier.BundleTotalPrice;

        return Math.Max(regularPriceForBundledUnits - bundlePriceForBundledUnits, 0m);
    }

    private static void Accumulate(Dictionary<Guid, (string Name, decimal Amount)> totals, Guid id, string name, decimal amount)
    {
        totals[id] = totals.TryGetValue(id, out var existing) ? (name, existing.Amount + amount) : (name, amount);
    }

    private static readonly Dictionary<Guid, string> EmptyUserNames = [];

    private static OrderDetailDto ToDetailDto(Order order, bool includeHistory = false, IReadOnlyDictionary<Guid, string>? userNames = null) => new(
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
        order.ReturnReason,
        order.CreatedAtUtc,
        order.Items
            .Select(i => new OrderItemDto(i.Id, i.ProductVariantId, i.ProductName, i.ProductSlug, i.ImageUrl, i.Color, i.Size, i.Sku, i.UnitPrice, i.Quantity, i.LineTotal))
            .ToList(),
        includeHistory
            ? order.StatusHistory
                .OrderBy(h => h.CreatedAtUtc)
                .Select(h => new OrderStatusHistoryDto(
                    h.Id, h.OldStatus, h.NewStatus, h.Reason, h.CreatedAtUtc,
                    h.ChangedByUserId.HasValue && (userNames ?? EmptyUserNames).TryGetValue(h.ChangedByUserId.Value, out var changedByName) ? changedByName : null))
                .ToList()
            : [],
        includeHistory
            ? order.CallAttempts
                .OrderBy(a => a.CalledAtUtc)
                .Select(a => new OrderCallAttemptDto(
                    a.Id, a.AttemptNumber, a.Result, a.Notes, a.CalledAtUtc, a.NextCallAtUtc,
                    (userNames ?? EmptyUserNames).TryGetValue(a.AgentUserId, out var agentName) ? agentName : null))
                .ToList()
            : [],
        order.AppliedPromotions
            .Select(p => new OrderPromotionDto(p.Id, p.PromotionId, p.PromotionName, p.DiscountAmount))
            .ToList(),
        ToShipmentDto(order.Shipment),
        ToMarketingAttributionDto(order),
        order.CreatedByUserId,
        order.ManualDiscountAmount,
        order.NegotiatedFreeShipping,
        EditableStatuses.Contains(order.Status));

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
