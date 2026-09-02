using Ecommerce.Application.Marketing;
using Ecommerce.Application.Marketing.Dtos;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Marketing;

public class MarketingService(AppDbContext dbContext) : IMarketingService
{
    public async Task<IReadOnlyList<MarketingSourceSummaryDto>> GetSourceSummaryAsync(int days, CancellationToken cancellationToken = default)
    {
        days = days is < 1 or > 365 ? 30 : days;
        var since = DateTime.UtcNow.AddDays(-days);

        var grouped = await dbContext.Orders.AsNoTracking()
            .Where(o => o.CreatedAtUtc >= since)
            .GroupBy(o => o.UtmSource)
            .Select(g => new { Source = g.Key, OrderCount = g.Count(), TotalRevenue = g.Sum(o => o.Total) })
            .ToListAsync(cancellationToken);

        return grouped
            .Select(g => new MarketingSourceSummaryDto(g.Source ?? "Direct", g.OrderCount, g.TotalRevenue))
            .OrderByDescending(s => s.OrderCount)
            .ToList();
    }
}
