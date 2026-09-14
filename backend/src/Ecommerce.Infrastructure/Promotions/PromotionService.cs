using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Promotions;
using Ecommerce.Application.Promotions.Dtos;
using Ecommerce.Domain.Promotions;
using Ecommerce.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Promotions;

public class PromotionService(
    AppDbContext dbContext,
    IValidator<SavePromotionRequest> validator) : IPromotionService
{
    public async Task<PromotionDetailDto> CreateAsync(SavePromotionRequest request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsureCouponCodeIsUniqueAsync(request.CouponCode, null, cancellationToken);

        var promotion = new Promotion
        {
            Name = request.Name,
            Description = request.Description,
            Type = request.Type,
            PercentageValue = request.PercentageValue,
            FixedAmountValue = request.FixedAmountValue,
            BuyQuantity = request.BuyQuantity,
            GetQuantity = request.GetQuantity,
            MinQuantity = request.MinQuantity,
            CouponCode = request.CouponCode,
            StartsAtUtc = request.StartsAtUtc,
            EndsAtUtc = request.EndsAtUtc,
            IsActive = request.IsActive,
            Priority = request.Priority,
        };

        ApplyProductAndCategoryLinks(promotion, request.ProductIds, request.CategoryIds);
        ApplyBundleTiers(promotion, request.BundleTiers);

        // The whole graph (promotion + its tiers/product-links/category-links) is tracked together via
        // this single Add on a genuinely new root entity, so EF correctly infers Added for everything
        // reachable from it — no Added-vs-Modified ambiguity here (see UpdateAsync for why that's NOT
        // true once the promotion is already tracked, e.g. after being loaded for an edit).
        dbContext.Promotions.Add(promotion);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(promotion.Id, cancellationToken);
    }

    public async Task<PromotionDetailDto> UpdateAsync(Guid id, SavePromotionRequest request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsureCouponCodeIsUniqueAsync(request.CouponCode, id, cancellationToken);

        var promotion = await dbContext.Promotions
            .Include(p => p.Products)
            .Include(p => p.Categories)
            .Include(p => p.BundleTiers)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundAppException("Promotion introuvable.");

        promotion.Name = request.Name;
        promotion.Description = request.Description;
        promotion.Type = request.Type;
        promotion.PercentageValue = request.PercentageValue;
        promotion.FixedAmountValue = request.FixedAmountValue;
        promotion.BuyQuantity = request.BuyQuantity;
        promotion.GetQuantity = request.GetQuantity;
        promotion.MinQuantity = request.MinQuantity;
        promotion.CouponCode = request.CouponCode;
        promotion.StartsAtUtc = request.StartsAtUtc;
        promotion.EndsAtUtc = request.EndsAtUtc;
        promotion.IsActive = request.IsActive;
        promotion.Priority = request.Priority;
        promotion.UpdatedAtUtc = DateTime.UtcNow;

        promotion.Products.Clear();
        promotion.Categories.Clear();
        ApplyProductAndCategoryLinks(promotion, request.ProductIds, request.CategoryIds);

        // Explicit DbSet remove/add rather than promotion.BundleTiers.Clear()/Add() — for an
        // already-tracked parent (every update, unlike CreateAsync's brand-new graph),
        // PromotionBundleTier's client-generated Guid Id makes EF's Added-vs-Modified heuristic
        // misfire and try to UPDATE a row that was never inserted (same DbUpdateConcurrencyException
        // class of bug as OrderService.RecalculateTotalsAsync hit with OrderItem/OrderPromotion —
        // see that method's comment for the full mechanism). Being explicit sidesteps the ambiguity.
        if (promotion.BundleTiers.Count > 0)
        {
            dbContext.PromotionBundleTiers.RemoveRange(promotion.BundleTiers);
            promotion.BundleTiers.Clear();
        }
        ApplyBundleTiers(promotion, request.BundleTiers, addToDbSetDirectly: true);

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(promotion.Id, cancellationToken);
    }

    public async Task<PromotionDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var promotion = await dbContext.Promotions.AsNoTracking()
            .Include(p => p.Products)
            .Include(p => p.Categories)
            .Include(p => p.BundleTiers)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundAppException("Promotion introuvable.");

        return ToDetailDto(promotion);
    }

    public async Task<PagedResult<PromotionDto>> GetPagedAsync(
        bool includeInactive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = dbContext.Promotions.AsNoTracking()
            .Include(p => p.Products)
            .Include(p => p.Categories)
            .Include(p => p.BundleTiers)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var entities = await query
            .OrderByDescending(p => p.Priority)
            .ThenByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<PromotionDto>(entities.Select(ToDto).ToList(), page, pageSize, totalCount);
    }

    public async Task<IReadOnlyList<PromotionDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        var entities = await dbContext.Promotions.AsNoTracking()
            .Include(p => p.Products)
            .Include(p => p.Categories)
            .Include(p => p.BundleTiers)
            .Where(p => p.IsActive && p.Type != PromotionType.Coupon && p.StartsAtUtc <= now && p.EndsAtUtc >= now)
            .OrderByDescending(p => p.Priority)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDto).ToList();
    }

    private async Task EnsureCouponCodeIsUniqueAsync(string? couponCode, Guid? excludingId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(couponCode))
        {
            return;
        }

        var exists = await dbContext.Promotions
            .AnyAsync(p => p.CouponCode == couponCode && p.Id != excludingId, cancellationToken);

        if (exists)
        {
            throw new ConflictAppException("Ce code promo est déjà utilisé par une autre promotion.");
        }
    }

    private static void ApplyProductAndCategoryLinks(Promotion promotion, IReadOnlyList<Guid> productIds, IReadOnlyList<Guid> categoryIds)
    {
        foreach (var productId in productIds.Distinct())
        {
            promotion.Products.Add(new PromotionProduct { PromotionId = promotion.Id, ProductId = productId });
        }

        foreach (var categoryId in categoryIds.Distinct())
        {
            promotion.Categories.Add(new PromotionCategory { PromotionId = promotion.Id, CategoryId = categoryId });
        }
    }

    private void ApplyBundleTiers(Promotion promotion, IReadOnlyList<BundleTierRequest>? tiers, bool addToDbSetDirectly = false)
    {
        foreach (var tier in tiers ?? [])
        {
            var entity = new PromotionBundleTier
            {
                PromotionId = promotion.Id,
                BundleQuantity = tier.BundleQuantity,
                BundleTotalPrice = tier.BundleTotalPrice,
                IncludesFreeShipping = tier.IncludesFreeShipping,
            };

            // See UpdateAsync's comment: on an already-tracked promotion, add directly to the DbSet
            // (relationship fixup then populates promotion.BundleTiers automatically) instead of the
            // collection navigation, to avoid EF's Added-vs-Modified ambiguity for the client-generated Id.
            if (addToDbSetDirectly)
            {
                dbContext.PromotionBundleTiers.Add(entity);
            }
            else
            {
                promotion.BundleTiers.Add(entity);
            }
        }
    }

    private static PromotionDto ToDto(Promotion p) => new(
        p.Id,
        p.Name,
        p.Description,
        p.Type,
        p.PercentageValue,
        p.FixedAmountValue,
        p.BuyQuantity,
        p.GetQuantity,
        !string.IsNullOrEmpty(p.CouponCode),
        p.StartsAtUtc,
        p.EndsAtUtc,
        p.IsActive,
        p.Priority,
        p.Products.Select(pp => pp.ProductId).ToList(),
        p.Categories.Select(pc => pc.CategoryId).ToList(),
        p.MinQuantity,
        ToBundleTierDtos(p));

    private static PromotionDetailDto ToDetailDto(Promotion p) => new(
        p.Id,
        p.Name,
        p.Description,
        p.Type,
        p.PercentageValue,
        p.FixedAmountValue,
        p.BuyQuantity,
        p.GetQuantity,
        p.CouponCode,
        p.StartsAtUtc,
        p.EndsAtUtc,
        p.IsActive,
        p.Priority,
        p.Products.Select(pp => pp.ProductId).ToList(),
        p.Categories.Select(pc => pc.CategoryId).ToList(),
        p.MinQuantity,
        ToBundleTierDtos(p));

    private static List<BundleTierDto> ToBundleTierDtos(Promotion p) =>
        p.BundleTiers
            .OrderBy(t => t.BundleQuantity)
            .Select(t => new BundleTierDto(t.Id, t.BundleQuantity, t.BundleTotalPrice, t.IncludesFreeShipping))
            .ToList();
}
