using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Inventory;
using Ecommerce.Application.Inventory.Dtos;
using Ecommerce.Domain.Inventory;
using Ecommerce.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Inventory;

public class InventoryService(
    AppDbContext dbContext,
    IValidator<RestockRequest> restockValidator,
    IValidator<AdjustInventoryRequest> adjustValidator) : IInventoryService
{
    // Matches DashboardService's own LowStockThreshold (part 21) — kept as a separate constant here
    // since there's no shared constants module in this codebase, but the two must stay in sync if
    // the business ever wants a different "low stock" cutoff.
    private const int LowStockThreshold = 5;

    // Shared variant-row shape for both GetOverviewAsync and GetByProductIdAsync — a flat .Select()
    // projection straight into a record constructor translates fine in EF Core (the established
    // GroupBy-into-record gotcha in this codebase only applies to GroupBy aggregates, not plain
    // per-row projections like this one).
    private record VariantRow(
        Guid ProductVariantId, Guid ProductId, string ProductName, string ProductSlug, Guid CategoryId, string CategoryName,
        string Color, string Size, string Sku, decimal? CostPrice, decimal Price,
        int AvailableQuantity, int ReservedQuantity, int SoldQuantity, int ReturnedQuantity, int DamagedQuantity);

    private static IQueryable<VariantRow> ProjectVariantRows(IQueryable<InventoryRecord> source) =>
        source.Select(i => new VariantRow(
            i.ProductVariantId,
            i.ProductVariant.ProductId,
            i.ProductVariant.Product.Name,
            i.ProductVariant.Product.Slug,
            i.ProductVariant.Product.CategoryId,
            i.ProductVariant.Product.Category.Name,
            i.ProductVariant.Color,
            i.ProductVariant.Size,
            i.ProductVariant.Sku,
            i.ProductVariant.CostPrice,
            i.ProductVariant.PriceOverride ?? i.ProductVariant.Product.Price,
            i.AvailableQuantity,
            i.ReservedQuantity,
            i.SoldQuantity,
            i.ReturnedQuantity,
            i.DamagedQuantity));

    private static InventoryVariantOverviewDto ToVariantDto(VariantRow v) =>
        new(v.ProductVariantId, v.Color, v.Size, v.Sku, v.CostPrice, v.Price, v.AvailableQuantity, v.ReservedQuantity, v.SoldQuantity, v.ReturnedQuantity, v.DamagedQuantity);

    public async Task<InventoryProductOverviewDto> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var variants = await ProjectVariantRows(
                dbContext.Inventory.AsNoTracking().Where(i => i.ProductVariant.IsActive && i.ProductVariant.ProductId == productId))
            .ToListAsync(cancellationToken);

        if (variants.Count == 0)
        {
            throw new NotFoundAppException("Produit introuvable ou sans variante active.");
        }

        var imageUrl = await dbContext.Products.AsNoTracking()
            .Where(p => p.Id == productId)
            .Select(p => p.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault()
                ?? p.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Url).FirstOrDefault())
            .FirstOrDefaultAsync(cancellationToken);

        var first = variants[0];
        return new InventoryProductOverviewDto(
            productId, first.ProductName, first.ProductSlug, first.CategoryId, first.CategoryName, imageUrl,
            variants.Select(ToVariantDto).ToList());
    }

    public async Task<PagedResult<InventoryProductOverviewDto>> GetOverviewAsync(
        InventoryOverviewFilter filter, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        // Step 1: find which PRODUCTS have at least one variant matching the filters (search/category/
        // stockStatus), and the lowest AvailableQuantity among those matching variants — used purely to
        // rank products "most needing attention first", the same ordering the earlier flat-variant
        // version used. Materialized into an anonymous type first (the established EF GroupBy-into-
        // record translation gotcha in this codebase) before mapping/sorting client-side.
        var variantQuery = dbContext.Inventory.AsNoTracking()
            .Where(i => i.ProductVariant.IsActive && i.ProductVariant.Product.IsActive);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            variantQuery = variantQuery.Where(i => i.ProductVariant.Product.Name.Contains(term) || i.ProductVariant.Sku.Contains(term));
        }

        if (filter.CategoryId.HasValue)
        {
            variantQuery = variantQuery.Where(i => i.ProductVariant.Product.CategoryId == filter.CategoryId.Value);
        }

        variantQuery = filter.StockStatus switch
        {
            InventoryStockStatus.Low => variantQuery.Where(i => i.AvailableQuantity > 0 && i.AvailableQuantity <= LowStockThreshold),
            InventoryStockStatus.Out => variantQuery.Where(i => i.AvailableQuantity <= 0),
            _ => variantQuery,
        };

        var matchingProducts = await variantQuery
            .GroupBy(i => i.ProductVariant.ProductId)
            .Select(g => new { ProductId = g.Key, MinAvailable = g.Min(i => i.AvailableQuantity) })
            .ToListAsync(cancellationToken);

        var orderedProductIds = matchingProducts
            .OrderBy(p => p.MinAvailable)
            .Select(p => p.ProductId)
            .ToList();

        var totalCount = orderedProductIds.Count;
        var pageProductIds = orderedProductIds.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        if (pageProductIds.Count == 0)
        {
            return new PagedResult<InventoryProductOverviewDto>([], page, pageSize, totalCount);
        }

        // Step 2: once a product matches, show ALL of its active variants (not just the one that
        // matched the filter) — an admin opening a product needs the full picture.
        var rawVariants = await ProjectVariantRows(
                dbContext.Inventory.AsNoTracking().Where(i => i.ProductVariant.IsActive && pageProductIds.Contains(i.ProductVariant.ProductId)))
            .ToListAsync(cancellationToken);

        var images = await dbContext.Products.AsNoTracking()
            .Where(p => pageProductIds.Contains(p.Id))
            .Select(p => new
            {
                p.Id,
                ImageUrl = p.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault()
                    ?? p.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Url).FirstOrDefault(),
            })
            .ToListAsync(cancellationToken);
        var imageByProductId = images.ToDictionary(x => x.Id, x => x.ImageUrl);

        var variantsByProductId = rawVariants
            .GroupBy(v => v.ProductId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var items = pageProductIds
            .Where(variantsByProductId.ContainsKey)
            .Select(productId =>
            {
                var variants = variantsByProductId[productId];
                var first = variants[0];
                return new InventoryProductOverviewDto(
                    productId,
                    first.ProductName,
                    first.ProductSlug,
                    first.CategoryId,
                    first.CategoryName,
                    imageByProductId.GetValueOrDefault(productId),
                    variants.Select(ToVariantDto).ToList());
            })
            .ToList();

        return new PagedResult<InventoryProductOverviewDto>(items, page, pageSize, totalCount);
    }

    public async Task<InventoryDto> GetByVariantIdAsync(Guid variantId, CancellationToken cancellationToken = default) =>
        await GetDtoAsync(variantId, cancellationToken);

    public async Task<IReadOnlyList<InventoryTransactionDto>> GetTransactionsAsync(Guid variantId, CancellationToken cancellationToken = default) =>
        await dbContext.InventoryTransactions.AsNoTracking()
            .Where(t => t.ProductVariantId == variantId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new InventoryTransactionDto(
                t.Id,
                t.ProductVariantId,
                t.Type.ToString().ToUpperInvariant(),
                t.Quantity,
                t.Reason,
                t.SupplierId,
                t.Supplier != null ? t.Supplier.Name : null,
                t.UnitCost,
                t.CreatedAtUtc))
            .ToListAsync(cancellationToken);

    public async Task<InventoryDto> RestockAsync(RestockRequest request, CancellationToken cancellationToken = default)
    {
        await restockValidator.ValidateAndThrowAsync(request, cancellationToken);

        if (request.SupplierId.HasValue && !await dbContext.Suppliers.AnyAsync(s => s.Id == request.SupplierId.Value, cancellationToken))
        {
            throw new NotFoundAppException("Fournisseur introuvable.");
        }

        var affected = await dbContext.Inventory
            .Where(i => i.ProductVariantId == request.ProductVariantId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.AvailableQuantity, i => i.AvailableQuantity + request.Quantity), cancellationToken);

        if (affected == 0)
        {
            throw new NotFoundAppException("Variante introuvable.");
        }

        // The variant's current cost price reflects the most recent purchase — costs can change
        // between restocks/suppliers, so this always overwrites rather than averaging.
        if (request.UnitCost.HasValue)
        {
            await dbContext.ProductVariants
                .Where(v => v.Id == request.ProductVariantId)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.CostPrice, request.UnitCost.Value), cancellationToken);
        }

        await LogTransactionAsync(request.ProductVariantId, InventoryTransactionType.Restock, request.Quantity, request.Reason, request.SupplierId, request.UnitCost, cancellationToken);

        return await GetDtoAsync(request.ProductVariantId, cancellationToken);
    }

    public async Task<InventoryDto> AdjustAsync(AdjustInventoryRequest request, CancellationToken cancellationToken = default)
    {
        await adjustValidator.ValidateAndThrowAsync(request, cancellationToken);

        var affected = await dbContext.Inventory
            .Where(i => i.ProductVariantId == request.ProductVariantId && i.AvailableQuantity + request.QuantityDelta >= 0)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.AvailableQuantity, i => i.AvailableQuantity + request.QuantityDelta), cancellationToken);

        if (affected == 0)
        {
            await EnsureVariantExistsAsync(request.ProductVariantId, cancellationToken);
            throw new ConflictAppException("L'ajustement rendrait le stock disponible négatif.");
        }

        await LogTransactionAsync(request.ProductVariantId, InventoryTransactionType.Adjustment, request.QuantityDelta, request.Reason, null, null, cancellationToken);

        return await GetDtoAsync(request.ProductVariantId, cancellationToken);
    }

    public async Task ReserveAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default)
    {
        var affected = await dbContext.Inventory
            .Where(i => i.ProductVariantId == variantId && i.AvailableQuantity >= quantity)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.AvailableQuantity, i => i.AvailableQuantity - quantity)
                .SetProperty(i => i.ReservedQuantity, i => i.ReservedQuantity + quantity), cancellationToken);

        if (affected == 0)
        {
            await EnsureVariantExistsAsync(variantId, cancellationToken);
            throw new ConflictAppException("Stock disponible insuffisant pour réserver cette quantité.");
        }

        await LogTransactionAsync(variantId, InventoryTransactionType.Reserve, quantity, null, null, null, cancellationToken);
    }

    public async Task ReleaseAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default)
    {
        var affected = await dbContext.Inventory
            .Where(i => i.ProductVariantId == variantId && i.ReservedQuantity >= quantity)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.ReservedQuantity, i => i.ReservedQuantity - quantity)
                .SetProperty(i => i.AvailableQuantity, i => i.AvailableQuantity + quantity), cancellationToken);

        if (affected == 0)
        {
            await EnsureVariantExistsAsync(variantId, cancellationToken);
            throw new ConflictAppException("Quantité réservée insuffisante pour cette libération.");
        }

        await LogTransactionAsync(variantId, InventoryTransactionType.Release, quantity, null, null, null, cancellationToken);
    }

    public async Task RecordSaleAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default)
    {
        var affected = await dbContext.Inventory
            .Where(i => i.ProductVariantId == variantId && i.ReservedQuantity >= quantity)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.ReservedQuantity, i => i.ReservedQuantity - quantity)
                .SetProperty(i => i.SoldQuantity, i => i.SoldQuantity + quantity), cancellationToken);

        if (affected == 0)
        {
            await EnsureVariantExistsAsync(variantId, cancellationToken);
            throw new ConflictAppException("Quantité réservée insuffisante pour finaliser la vente.");
        }

        await LogTransactionAsync(variantId, InventoryTransactionType.Sale, quantity, null, null, null, cancellationToken);
    }

    public async Task RecordReturnAsync(Guid variantId, int quantity, bool isDamaged = false, CancellationToken cancellationToken = default)
    {
        var affected = isDamaged
            ? await dbContext.Inventory
                .Where(i => i.ProductVariantId == variantId && i.SoldQuantity >= quantity)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(i => i.SoldQuantity, i => i.SoldQuantity - quantity)
                    .SetProperty(i => i.DamagedQuantity, i => i.DamagedQuantity + quantity), cancellationToken)
            : await dbContext.Inventory
                .Where(i => i.ProductVariantId == variantId && i.SoldQuantity >= quantity)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(i => i.SoldQuantity, i => i.SoldQuantity - quantity)
                    .SetProperty(i => i.ReturnedQuantity, i => i.ReturnedQuantity + quantity), cancellationToken);

        if (affected == 0)
        {
            await EnsureVariantExistsAsync(variantId, cancellationToken);
            throw new ConflictAppException("Quantité vendue insuffisante pour enregistrer ce retour.");
        }

        await LogTransactionAsync(variantId, InventoryTransactionType.Return, quantity, null, null, null, cancellationToken);
    }

    public async Task ReleaseToDamagedAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default)
    {
        var affected = await dbContext.Inventory
            .Where(i => i.ProductVariantId == variantId && i.ReservedQuantity >= quantity)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.ReservedQuantity, i => i.ReservedQuantity - quantity)
                .SetProperty(i => i.DamagedQuantity, i => i.DamagedQuantity + quantity), cancellationToken);

        if (affected == 0)
        {
            await EnsureVariantExistsAsync(variantId, cancellationToken);
            throw new ConflictAppException("Quantité réservée insuffisante pour cette opération.");
        }

        await LogTransactionAsync(variantId, InventoryTransactionType.Return, quantity, null, null, null, cancellationToken);
    }

    public async Task MarkAvailableDamagedAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default)
    {
        var affected = await dbContext.Inventory
            .Where(i => i.ProductVariantId == variantId && i.AvailableQuantity >= quantity)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.AvailableQuantity, i => i.AvailableQuantity - quantity)
                .SetProperty(i => i.DamagedQuantity, i => i.DamagedQuantity + quantity), cancellationToken);

        if (affected == 0)
        {
            await EnsureVariantExistsAsync(variantId, cancellationToken);
            throw new ConflictAppException("Quantité disponible insuffisante pour cette opération.");
        }

        await LogTransactionAsync(variantId, InventoryTransactionType.Return, quantity, null, null, null, cancellationToken);
    }

    private async Task EnsureVariantExistsAsync(Guid variantId, CancellationToken cancellationToken)
    {
        if (!await dbContext.Inventory.AnyAsync(i => i.ProductVariantId == variantId, cancellationToken))
        {
            throw new NotFoundAppException("Variante introuvable.");
        }
    }

    private async Task LogTransactionAsync(
        Guid variantId,
        InventoryTransactionType type,
        int quantity,
        string? reason,
        Guid? supplierId,
        decimal? unitCost,
        CancellationToken cancellationToken)
    {
        dbContext.InventoryTransactions.Add(new InventoryTransaction
        {
            ProductVariantId = variantId,
            Type = type,
            Quantity = quantity,
            Reason = reason,
            SupplierId = supplierId,
            UnitCost = unitCost,
        });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<InventoryDto> GetDtoAsync(Guid variantId, CancellationToken cancellationToken)
    {
        var dto = await dbContext.Inventory.AsNoTracking()
            .Where(i => i.ProductVariantId == variantId)
            .Select(i => new InventoryDto(
                i.ProductVariantId,
                i.ProductVariant.Sku,
                i.AvailableQuantity,
                i.ReservedQuantity,
                i.SoldQuantity,
                i.ReturnedQuantity,
                i.DamagedQuantity))
            .FirstOrDefaultAsync(cancellationToken);

        return dto ?? throw new NotFoundAppException("Variante introuvable.");
    }
}
