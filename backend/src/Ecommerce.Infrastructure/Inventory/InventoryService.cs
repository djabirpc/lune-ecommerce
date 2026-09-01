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
    public async Task<InventoryDto> GetByVariantIdAsync(Guid variantId, CancellationToken cancellationToken = default) =>
        await GetDtoAsync(variantId, cancellationToken);

    public async Task<IReadOnlyList<InventoryTransactionDto>> GetTransactionsAsync(Guid variantId, CancellationToken cancellationToken = default) =>
        await dbContext.InventoryTransactions.AsNoTracking()
            .Where(t => t.ProductVariantId == variantId)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new InventoryTransactionDto(t.Id, t.ProductVariantId, t.Type.ToString().ToUpperInvariant(), t.Quantity, t.Reason, t.CreatedAtUtc))
            .ToListAsync(cancellationToken);

    public async Task<InventoryDto> RestockAsync(RestockRequest request, CancellationToken cancellationToken = default)
    {
        await restockValidator.ValidateAndThrowAsync(request, cancellationToken);

        var affected = await dbContext.Inventory
            .Where(i => i.ProductVariantId == request.ProductVariantId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.AvailableQuantity, i => i.AvailableQuantity + request.Quantity), cancellationToken);

        if (affected == 0)
        {
            throw new NotFoundAppException("Variante introuvable.");
        }

        await LogTransactionAsync(request.ProductVariantId, InventoryTransactionType.Restock, request.Quantity, request.Reason, cancellationToken);

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

        await LogTransactionAsync(request.ProductVariantId, InventoryTransactionType.Adjustment, request.QuantityDelta, request.Reason, cancellationToken);

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

        await LogTransactionAsync(variantId, InventoryTransactionType.Reserve, quantity, null, cancellationToken);
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

        await LogTransactionAsync(variantId, InventoryTransactionType.Release, quantity, null, cancellationToken);
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

        await LogTransactionAsync(variantId, InventoryTransactionType.Sale, quantity, null, cancellationToken);
    }

    public async Task RecordReturnAsync(Guid variantId, int quantity, CancellationToken cancellationToken = default)
    {
        var affected = await dbContext.Inventory
            .Where(i => i.ProductVariantId == variantId && i.SoldQuantity >= quantity)
            .ExecuteUpdateAsync(s => s
                .SetProperty(i => i.SoldQuantity, i => i.SoldQuantity - quantity)
                .SetProperty(i => i.ReturnedQuantity, i => i.ReturnedQuantity + quantity), cancellationToken);

        if (affected == 0)
        {
            await EnsureVariantExistsAsync(variantId, cancellationToken);
            throw new ConflictAppException("Quantité vendue insuffisante pour enregistrer ce retour.");
        }

        await LogTransactionAsync(variantId, InventoryTransactionType.Return, quantity, null, cancellationToken);
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
        CancellationToken cancellationToken)
    {
        dbContext.InventoryTransactions.Add(new InventoryTransaction
        {
            ProductVariantId = variantId,
            Type = type,
            Quantity = quantity,
            Reason = reason,
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
