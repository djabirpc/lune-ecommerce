using System.Globalization;
using Ecommerce.Application.Shipping;
using Ecommerce.Application.Shipping.Dtos;
using Ecommerce.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.Api.Controllers;

/// <summary>
/// Admin-only, read-only preview of the merchant's real 48h Express (Ecotrack) account — lets an
/// admin browse packages already created directly on the Ecotrack platform before deciding whether/how
/// to wire the real integration into Luna's own order flow. Every action here is a GET against
/// Ecotrack; nothing here ever creates, updates, or deletes anything on Ecotrack or in Luna's own
/// database. Not linked from any customer-facing route.
/// </summary>
[ApiController]
[Route("api/ecotrack-preview")]
[Authorize(Roles = Roles.OrderManagers)]
public class Ecotrack48hPreviewController(IEcotrack48hPreviewService previewService) : ControllerBase
{
    [HttpGet("orders")]
    public async Task<ActionResult<Ecotrack48hPreviewOrdersPageDto>> GetOrders(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromQuery] string? status,
        [FromQuery] int? wilayaId,
        [FromQuery] string? startDate,
        [FromQuery] string? endDate,
        [FromQuery] decimal? minAmount,
        [FromQuery] decimal? maxAmount,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var filter = new Ecotrack48hPreviewFilter(status, wilayaId, ParseDate(startDate), ParseDate(endDate, endOfDay: true), minAmount, maxAmount, search);

        return Ok(await previewService.GetOrdersAsync(
            filter,
            page < 1 ? 1 : page,
            pageSize is < 1 or > 100 ? 20 : pageSize,
            cancellationToken));
    }

    [HttpGet("orders/{tracking}")]
    public async Task<ActionResult<Ecotrack48hPreviewTrackingDto>> GetTracking(string tracking, CancellationToken cancellationToken)
    {
        return Ok(await previewService.GetTrackingAsync(tracking, cancellationToken));
    }

    [HttpGet("orders/{tracking}/label")]
    public async Task<IActionResult> GetLabel(string tracking, CancellationToken cancellationToken)
    {
        var pdf = await previewService.GetLabelPdfAsync(tracking, cancellationToken);
        return File(pdf, "application/pdf", $"{tracking}.pdf");
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<Ecotrack48hPreviewDashboardDto>> GetDashboard(
        [FromQuery] string? startDate,
        [FromQuery] string? endDate,
        [FromQuery] decimal? minAmount,
        [FromQuery] decimal? maxAmount,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        // Status/WilayaId deliberately omitted here — see Ecotrack48hPreviewFilter's doc comment.
        var filter = new Ecotrack48hPreviewFilter(
            StartDate: ParseDate(startDate),
            EndDate: ParseDate(endDate, endOfDay: true),
            MinAmount: minAmount,
            MaxAmount: maxAmount,
            Search: search);

        return Ok(await previewService.GetDashboardAsync(filter, cancellationToken));
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
