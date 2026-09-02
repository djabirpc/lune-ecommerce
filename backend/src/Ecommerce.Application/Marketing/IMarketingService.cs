using Ecommerce.Application.Marketing.Dtos;

namespace Ecommerce.Application.Marketing;

public interface IMarketingService
{
    /// <summary>Orders grouped by UtmSource (orders with no source are bucketed as "Direct"), over the last N days.</summary>
    Task<IReadOnlyList<MarketingSourceSummaryDto>> GetSourceSummaryAsync(int days, CancellationToken cancellationToken = default);
}
