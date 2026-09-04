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
            CouponCode = request.CouponCode,
            StartsAtUtc = request.StartsAtUtc,
            EndsAtUtc = request.EndsAtUtc,
            IsActive = request.IsActive,
            Priority = request.Priority,
        };

        ApplyProductAndCategoryLinks(promotion, request.ProductIds, request.CategoryIds);

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
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundAppException("Promotion introuvable.");

        promotion.Name = request.Name;
        promotion.Description = request.Description;
        promotion.Type = request.Type;
        promotion.PercentageValue = request.PercentageValue;
        promotion.FixedAmountValue = request.FixedAmountValue;
        promotion.BuyQuantity = request.BuyQuantity;
        promotion.GetQuantity = request.GetQuantity;
        promotion.CouponCode = request.CouponCode;
        promotion.StartsAtUtc = request.StartsAtUtc;
        promotion.EndsAtUtc = request.EndsAtUtc;
        promotion.IsActive = request.IsActive;
        promotion.Priority = request.Priority;
        promotion.UpdatedAtUtc = DateTime.UtcNow;

        promotion.Products.Clear();
        promotion.Categories.Clear();
        ApplyProductAndCategoryLinks(promotion, request.ProductIds, request.CategoryIds);

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetByIdAsync(promotion.Id, cancellationToken);
    }

    public async Task<PromotionDetailDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var promotion = await dbContext.Promotions.AsNoTracking()
            .Include(p => p.Products)
            .Include(p => p.Categories)
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
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.Priority)
            .ThenByDescending(p => p.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => ToDto(p))
            .ToListAsync(cancellationToken);

        return new PagedResult<PromotionDto>(items, page, pageSize, totalCount);
    }

    public async Task<IReadOnlyList<PromotionDto>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        return await dbContext.Promotions.AsNoTracking()
            .Include(p => p.Products)
            .Include(p => p.Categories)
            .Where(p => p.IsActive && p.Type != PromotionType.Coupon && p.StartsAtUtc <= now && p.EndsAtUtc >= now)
            .OrderByDescending(p => p.Priority)
            .Select(p => ToDto(p))
            .ToListAsync(cancellationToken);
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
        p.Categories.Select(pc => pc.CategoryId).ToList());

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
        p.Categories.Select(pc => pc.CategoryId).ToList());
}
