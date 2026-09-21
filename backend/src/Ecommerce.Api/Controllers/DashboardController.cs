using System.Globalization;
using Ecommerce.Application.Dashboard;
using Ecommerce.Application.Dashboard.Dtos;
using Ecommerce.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

/// <summary>
/// Backs the admin "Vue Générale" dashboard. All filtering/aggregation happens server-side — see
/// DashboardService's doc comment for the per-metric business-date decisions.
/// </summary>
[ApiController]
[Route("api/dashboard")]
[Authorize(Roles = Roles.OrderManagers)]
public class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<ActionResult<DashboardOverviewDto>> GetOverview(
        [FromQuery] string? from,
        [FromQuery] string? to,
        CancellationToken cancellationToken)
    {
        var toDate = ParseDate(to, endOfDay: true) ?? DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);
        var fromDate = ParseDate(from) ?? new DateTime(toDate.Year, toDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        return Ok(await dashboardService.GetOverviewAsync(fromDate, toDate, cancellationToken));
    }

    private static DateTime? ParseDate(string? value, bool endOfDay = false)
    {
        if (!DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return null;
        }

        var utc = DateTime.SpecifyKind(date, DateTimeKind.Utc);
        return endOfDay ? utc.AddDays(1).AddTicks(-1) : utc;
    }
}
