using Ecommerce.Application.Common;
using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Suppliers;
using Ecommerce.Application.Suppliers.Dtos;
using Ecommerce.Domain.Inventory;
using Ecommerce.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Inventory;

public class SupplierService(
    AppDbContext dbContext,
    IValidator<SaveSupplierRequest> validator) : ISupplierService
{
    public async Task<PagedResult<SupplierDto>> GetPagedAsync(
        bool includeInactive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = dbContext.Suppliers.AsNoTracking();

        if (!includeInactive)
        {
            query = query.Where(s => s.IsActive);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => ToDto(s))
            .ToListAsync(cancellationToken);

        return new PagedResult<SupplierDto>(items, page, pageSize, totalCount);
    }

    public async Task<SupplierDto> CreateAsync(SaveSupplierRequest request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var supplier = new Supplier
        {
            Name = request.Name,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            Notes = request.Notes,
            IsActive = request.IsActive,
        };

        dbContext.Suppliers.Add(supplier);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(supplier);
    }

    public async Task<SupplierDto> UpdateAsync(Guid id, SaveSupplierRequest request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var supplier = await dbContext.Suppliers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new NotFoundAppException("Fournisseur introuvable.");

        supplier.Name = request.Name;
        supplier.Phone = request.Phone;
        supplier.Email = request.Email;
        supplier.Address = request.Address;
        supplier.Notes = request.Notes;
        supplier.IsActive = request.IsActive;
        supplier.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(supplier);
    }

    private static SupplierDto ToDto(Supplier s) =>
        new(s.Id, s.Name, s.Phone, s.Email, s.Address, s.Notes, s.IsActive);
}
