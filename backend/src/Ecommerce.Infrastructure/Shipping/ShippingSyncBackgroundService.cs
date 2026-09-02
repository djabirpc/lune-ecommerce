using Ecommerce.Application.Shipping;
using Ecommerce.Domain.Shipping;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecommerce.Infrastructure.Shipping;

/// <summary>
/// CLAUDE.md section 19: no carrier here supports webhooks, so tracking is refreshed via a
/// background poll at a configurable interval — deliberately not aggressive (default 5 minutes).
/// Only shipments in a non-terminal normalized status are synced; Yalidine/ZRExpress shipments
/// would fail (not implemented) but none can exist yet since those providers can't create shipments.
/// </summary>
public class ShippingSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<ShippingSyncOptions> options,
    ILogger<ShippingSyncBackgroundService> logger) : BackgroundService
{
    private static readonly NormalizedShippingStatus[] TerminalStatuses =
    [
        NormalizedShippingStatus.Delivered,
        NormalizedShippingStatus.Cancelled,
        NormalizedShippingStatus.Returned,
        NormalizedShippingStatus.Refused,
        NormalizedShippingStatus.Failed,
    ];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            logger.LogInformation("Shipping tracking sync is disabled (ShippingSync:Enabled=false).");
            return;
        }

        var interval = TimeSpan.FromSeconds(Math.Max(options.Value.IntervalSeconds, 30));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAllPendingShipmentsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Shipping tracking sync cycle failed.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task SyncAllPendingShipmentsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var shippingService = scope.ServiceProvider.GetRequiredService<IShippingService>();

        var pendingShipmentIds = await dbContext.Shipments
            .Where(s => !TerminalStatuses.Contains(s.NormalizedStatus))
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        foreach (var shipmentId in pendingShipmentIds)
        {
            try
            {
                await shippingService.SyncTrackingAsync(shipmentId, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Failed to sync tracking for shipment {ShipmentId}.", shipmentId);
            }
        }
    }
}
