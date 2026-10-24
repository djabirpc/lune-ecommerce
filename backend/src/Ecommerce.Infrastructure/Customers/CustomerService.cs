using Ecommerce.Application.Common;
using Ecommerce.Application.Customers;
using Ecommerce.Application.Customers.Dtos;
using Ecommerce.Domain.Orders;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Customers;

/// <summary>
/// No Customer entity exists (guest checkout, CLAUDE.md section 11) — every "customer" here is
/// reconstructed by grouping Orders by Phone. Grouping-with-most-recent-row-per-group doesn't
/// translate cleanly through EF Core's GroupBy, so (same pattern as InventoryService's product
/// overview) the relevant columns are projected and pulled into memory first, then grouped/sorted
/// in C# — order volume for a single COD shop is small enough that this is simpler than fighting
/// the translator, not a performance risk.
/// </summary>
public class CustomerService(AppDbContext dbContext) : ICustomerService
{
    private static readonly HashSet<OrderStatus> CancelledOrRefusedStatuses = [OrderStatus.Cancelled, OrderStatus.Refused];

    public async Task<PagedResult<CustomerSummaryDto>> GetPagedAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 20 : pageSize;

        var query = dbContext.Orders.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(o =>
                o.Phone.Contains(search) ||
                (o.FirstName + " " + o.LastName).Contains(search));
        }

        var rows = await query
            .Select(o => new
            {
                o.Phone,
                o.FirstName,
                o.LastName,
                o.Wilaya,
                o.Status,
                o.Total,
                o.CreatedAtUtc,
            })
            .ToListAsync(cancellationToken);

        var customers = rows
            .GroupBy(o => o.Phone)
            .Select(g =>
            {
                var mostRecent = g.OrderByDescending(o => o.CreatedAtUtc).First();
                return new CustomerSummaryDto(
                    g.Key,
                    $"{mostRecent.FirstName} {mostRecent.LastName}".Trim(),
                    mostRecent.Wilaya,
                    g.Count(),
                    g.Count(o => o.Status == OrderStatus.Delivered),
                    g.Count(o => CancelledOrRefusedStatuses.Contains(o.Status)),
                    g.Count(o => o.Status == OrderStatus.CustomerUnreachable),
                    g.Where(o => o.Status == OrderStatus.Delivered).Sum(o => o.Total),
                    g.Min(o => o.CreatedAtUtc),
                    g.Max(o => o.CreatedAtUtc));
            })
            .OrderByDescending(c => c.LastOrderAtUtc)
            .ToList();

        var totalCount = customers.Count;
        var pageItems = customers.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return new PagedResult<CustomerSummaryDto>(pageItems, page, pageSize, totalCount);
    }
}
