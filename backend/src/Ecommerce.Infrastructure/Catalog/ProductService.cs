using Ecommerce.Application.Catalog;
using Ecommerce.Application.Catalog.Dtos;
using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Domain.Catalog;
using Ecommerce.Domain.Inventory;
using Ecommerce.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Catalog;

public class ProductService(
    AppDbContext dbContext,
    IFileStorageService fileStorageService,
    IValidator<CreateProductRequest> createValidator,
    IValidator<UpdateProductRequest> updateValidator,
    IValidator<CreateProductVariantRequest> variantValidator) : IProductService
{
    private static readonly string[] AllowedImageContentTypes = ["image/jpeg", "image/png", "image/webp"];
    private const long MaxImageSizeBytes = 5 * 1024 * 1024;

    public async Task<ProductImageDto> AddImageAsync(
        Guid productId,
        UploadFileRequest file,
        string? altText,
        bool isPrimary,
        CancellationToken cancellationToken = default)
    {
        if (file.LengthBytes <= 0)
        {
            throw new ValidationAppException("Aucun fichier n'a été fourni.");
        }

        if (!AllowedImageContentTypes.Contains(file.ContentType))
        {
            throw new ValidationAppException("Format d'image non supporté. Utilisez JPEG, PNG ou WebP.");
        }

        if (file.LengthBytes > MaxImageSizeBytes)
        {
            throw new ValidationAppException("L'image dépasse la taille maximale autorisée (5 Mo).");
        }

        var product = await dbContext.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken)
            ?? throw new NotFoundAppException("Produit introuvable.");

        var url = await fileStorageService.SaveAsync(file, cancellationToken);

        var makePrimary = isPrimary || product.Images.Count == 0;
        if (makePrimary)
        {
            foreach (var existingImage in product.Images)
            {
                existingImage.IsPrimary = false;
            }
        }

        var image = new ProductImage
        {
            ProductId = productId,
            Url = url,
            AltText = altText,
            DisplayOrder = product.Images.Count == 0 ? 0 : product.Images.Max(i => i.DisplayOrder) + 1,
            IsPrimary = makePrimary,
        };

        dbContext.ProductImages.Add(image);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new ProductImageDto(image.Id, image.Url, image.AltText, image.DisplayOrder, image.IsPrimary);
    }

    public async Task DeleteImageAsync(Guid productId, Guid imageId, CancellationToken cancellationToken = default)
    {
        var image = await dbContext.ProductImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId, cancellationToken)
            ?? throw new NotFoundAppException("Image introuvable.");

        var wasPrimary = image.IsPrimary;
        dbContext.ProductImages.Remove(image);
        await dbContext.SaveChangesAsync(cancellationToken);

        await fileStorageService.DeleteAsync(image.Url, cancellationToken);

        if (wasPrimary)
        {
            var nextImage = await dbContext.ProductImages
                .Where(i => i.ProductId == productId)
                .OrderBy(i => i.DisplayOrder)
                .FirstOrDefaultAsync(cancellationToken);

            if (nextImage is not null)
            {
                nextImage.IsPrimary = true;
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }

    public async Task<ProductImageDto> SetPrimaryImageAsync(Guid productId, Guid imageId, CancellationToken cancellationToken = default)
    {
        var images = await dbContext.ProductImages
            .Where(i => i.ProductId == productId)
            .ToListAsync(cancellationToken);

        var target = images.FirstOrDefault(i => i.Id == imageId)
            ?? throw new NotFoundAppException("Image introuvable.");

        foreach (var image in images)
        {
            image.IsPrimary = image.Id == imageId;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ProductImageDto(target.Id, target.Url, target.AltText, target.DisplayOrder, true);
    }

    public async Task<PagedResult<ProductListItemDto>> GetPagedAsync(
        string? categorySlug,
        int page,
        int pageSize,
        bool includeInactive = false,
        bool sortByNewest = false,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = dbContext.Products.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            query = query.Where(p => p.Category.Slug == categorySlug);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        query = sortByNewest ? query.OrderByDescending(p => p.CreatedAtUtc) : query.OrderBy(p => p.Name);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductListItemDto(
                p.Id,
                p.Name,
                p.Slug,
                p.Price,
                p.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault(),
                p.Category.Name,
                p.Category.Slug,
                p.IsActive))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductListItemDto>(items, page, pageSize, totalCount);
    }

    public async Task<ProductDetailDto> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products.AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Images)
            .Include(p => p.Variants).ThenInclude(v => v.Inventory)
            .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken)
            ?? throw new NotFoundAppException("Produit introuvable.");

        return ToDetailDto(product);
    }

    public async Task<ProductDetailDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        if (!await dbContext.Categories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken))
        {
            throw new NotFoundAppException("Catégorie introuvable.");
        }

        if (await dbContext.Products.AnyAsync(p => p.Slug == request.Slug, cancellationToken))
        {
            throw new ConflictAppException("Un produit avec ce slug existe déjà.");
        }

        var skus = request.Variants.Select(v => v.Sku).ToList();
        if (await dbContext.ProductVariants.AnyAsync(v => skus.Contains(v.Sku), cancellationToken))
        {
            throw new ConflictAppException("Une ou plusieurs variantes ont un SKU déjà utilisé.");
        }

        var product = new Product
        {
            CategoryId = request.CategoryId,
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            Price = request.Price,
        };

        foreach (var variantRequest in request.Variants)
        {
            var variant = new ProductVariant
            {
                Color = variantRequest.Color,
                Size = variantRequest.Size,
                Sku = variantRequest.Sku,
                PriceOverride = variantRequest.PriceOverride,
                Inventory = new InventoryRecord { AvailableQuantity = variantRequest.InitialQuantity },
            };
            product.Variants.Add(variant);
        }

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var variant in product.Variants.Where(v => v.Inventory!.AvailableQuantity > 0))
        {
            dbContext.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductVariantId = variant.Id,
                Type = InventoryTransactionType.Restock,
                Quantity = variant.Inventory!.AvailableQuantity,
                Reason = "Stock initial à la création du produit.",
            });
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetBySlugAsync(product.Slug, cancellationToken);
    }

    public async Task<ProductDetailDto> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var product = await dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new NotFoundAppException("Produit introuvable.");

        if (!await dbContext.Categories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken))
        {
            throw new NotFoundAppException("Catégorie introuvable.");
        }

        if (await dbContext.Products.AnyAsync(p => p.Slug == request.Slug && p.Id != id, cancellationToken))
        {
            throw new ConflictAppException("Un produit avec ce slug existe déjà.");
        }

        product.CategoryId = request.CategoryId;
        product.Name = request.Name;
        product.Slug = request.Slug;
        product.Description = request.Description;
        product.Price = request.Price;
        product.IsActive = request.IsActive;
        product.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetBySlugAsync(product.Slug, cancellationToken);
    }

    public async Task<ProductVariantDto> AddVariantAsync(
        Guid productId,
        CreateProductVariantRequest request,
        CancellationToken cancellationToken = default)
    {
        await variantValidator.ValidateAndThrowAsync(request, cancellationToken);

        var product = await dbContext.Products.AnyAsync(p => p.Id == productId, cancellationToken);
        if (!product)
        {
            throw new NotFoundAppException("Produit introuvable.");
        }

        if (await dbContext.ProductVariants.AnyAsync(v => v.Sku == request.Sku, cancellationToken))
        {
            throw new ConflictAppException("Une variante avec ce SKU existe déjà.");
        }

        var variant = new ProductVariant
        {
            ProductId = productId,
            Color = request.Color,
            Size = request.Size,
            Sku = request.Sku,
            PriceOverride = request.PriceOverride,
            Inventory = new InventoryRecord { AvailableQuantity = request.InitialQuantity },
        };

        dbContext.ProductVariants.Add(variant);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (request.InitialQuantity > 0)
        {
            dbContext.InventoryTransactions.Add(new InventoryTransaction
            {
                ProductVariantId = variant.Id,
                Type = InventoryTransactionType.Restock,
                Quantity = request.InitialQuantity,
                Reason = "Stock initial à la création de la variante.",
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new ProductVariantDto(
            variant.Id,
            variant.Color,
            variant.Size,
            variant.Sku,
            variant.PriceOverride ?? (await dbContext.Products.Where(p => p.Id == productId).Select(p => p.Price).FirstAsync(cancellationToken)),
            variant.IsActive,
            variant.Inventory!.AvailableQuantity);
    }

    private static ProductDetailDto ToDetailDto(Product product) => new(
        product.Id,
        product.Name,
        product.Slug,
        product.Description,
        product.Price,
        product.IsActive,
        product.CategoryId,
        product.Category.Name,
        product.Category.Slug,
        product.Images
            .OrderBy(i => i.DisplayOrder)
            .Select(i => new ProductImageDto(i.Id, i.Url, i.AltText, i.DisplayOrder, i.IsPrimary))
            .ToList(),
        product.Variants
            .Select(v => new ProductVariantDto(
                v.Id,
                v.Color,
                v.Size,
                v.Sku,
                v.PriceOverride ?? product.Price,
                v.IsActive,
                v.Inventory?.AvailableQuantity ?? 0))
            .ToList());
}
