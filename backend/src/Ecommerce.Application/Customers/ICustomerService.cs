using Ecommerce.Application.Common;
using Ecommerce.Application.Customers.Dtos;

namespace Ecommerce.Application.Customers;

public interface ICustomerService
{
    /// <summary>One row per distinct phone number across all orders, sorted by most recent order
    /// first. <paramref name="search"/> matches against phone or name (partial, case-insensitive).</summary>
    Task<PagedResult<CustomerSummaryDto>> GetPagedAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}
