using Ecommerce.Application.Common;
using Ecommerce.Application.Suppliers.Dtos;

namespace Ecommerce.Application.Suppliers;

public interface ISupplierService
{
    Task<PagedResult<SupplierDto>> GetPagedAsync(
        bool includeInactive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<SupplierDto> CreateAsync(SaveSupplierRequest request, CancellationToken cancellationToken = default);

    Task<SupplierDto> UpdateAsync(Guid id, SaveSupplierRequest request, CancellationToken cancellationToken = default);
}
