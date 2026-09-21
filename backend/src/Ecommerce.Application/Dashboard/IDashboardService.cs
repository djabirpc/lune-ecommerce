using Ecommerce.Application.Dashboard.Dtos;

namespace Ecommerce.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardOverviewDto> GetOverviewAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
}
