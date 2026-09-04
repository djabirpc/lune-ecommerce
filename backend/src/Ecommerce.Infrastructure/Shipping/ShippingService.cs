using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Orders;
using Ecommerce.Application.Orders.Dtos;
using Ecommerce.Application.Shipping;
using Ecommerce.Application.Shipping.Dtos;
using Ecommerce.Domain.Orders;
using Ecommerce.Domain.Shipping;
using Ecommerce.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Shipping;

public class ShippingService(
    AppDbContext dbContext,
    IEnumerable<IShippingProvider> providers,
    IOrderService orderService) : IShippingService
{
    private readonly Dictionary<ShippingCarrier, IShippingProvider> _providers = providers.ToDictionary(p => p.Carrier);

    /// <summary>
    /// Normalized shipment statuses that should also drive the order status machine — reuses the
    /// existing OrderService.ChangeStatusAsync transitions rather than duplicating them. Statuses
    /// like InTransit/AtDestination have no matching order status (the order just stays "Shipped"
    /// through carrier transit) and are intentionally absent here.
    /// </summary>
    private static readonly Dictionary<NormalizedShippingStatus, OrderStatus> StatusToOrderTransition = new()
    {
        [NormalizedShippingStatus.OutForDelivery] = OrderStatus.OutForDelivery,
        [NormalizedShippingStatus.Delivered] = OrderStatus.Delivered,
        [NormalizedShippingStatus.Failed] = OrderStatus.DeliveryFailed,
        [NormalizedShippingStatus.Refused] = OrderStatus.Refused,
        [NormalizedShippingStatus.Returned] = OrderStatus.Returned,
        [NormalizedShippingStatus.Cancelled] = OrderStatus.Cancelled,
    };

    public async Task<ShipmentDto> CreateShipmentAsync(Guid orderId, CreateShipmentRequest request, CancellationToken cancellationToken = default)
    {
        var order = await dbContext.Orders
            .Include(o => o.Shipment)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new NotFoundAppException("Commande introuvable.");

        if (order.Status != OrderStatus.ReadyToShip)
        {
            throw new ConflictAppException("La commande doit être au statut \"Prête à expédier\" avant de créer une expédition.");
        }

        if (order.Shipment is not null)
        {
            throw new ConflictAppException("Une expédition existe déjà pour cette commande.");
        }

        var provider = GetProvider(request.Carrier);

        var result = await provider.CreateShipmentAsync(
            new ShipmentRequest(order.OrderNumber, order.FirstName, order.LastName, order.Phone, order.Wilaya, order.Commune, order.Address, order.Total),
            cancellationToken);

        var shipment = new Shipment
        {
            OrderId = order.Id,
            Carrier = request.Carrier,
            ProviderShipmentId = result.ProviderShipmentId,
            TrackingNumber = result.TrackingNumber,
            ProviderStatus = result.ProviderStatus,
            NormalizedStatus = result.NormalizedStatus,
        };
        shipment.TrackingEvents.Add(new ShipmentTrackingEvent
        {
            ProviderStatus = result.ProviderStatus,
            NormalizedStatus = result.NormalizedStatus,
            Description = "Expédition créée",
        });

        dbContext.Shipments.Add(shipment);
        await dbContext.SaveChangesAsync(cancellationToken);

        await orderService.ChangeStatusAsync(
            orderId,
            new ChangeOrderStatusRequest(OrderStatus.Shipped, $"Expédition créée ({request.Carrier})"),
            null,
            cancellationToken);

        return await GetShipmentDtoAsync(shipment.Id, cancellationToken);
    }

    public async Task<ShipmentDto> SyncTrackingAsync(Guid shipmentId, CancellationToken cancellationToken = default)
    {
        var shipment = await dbContext.Shipments
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken)
            ?? throw new NotFoundAppException("Expédition introuvable.");

        var provider = GetProvider(shipment.Carrier);
        var tracking = await provider.GetTrackingAsync(shipment.ProviderShipmentId, cancellationToken);

        if (tracking.NormalizedStatus == shipment.NormalizedStatus && tracking.ProviderStatus == shipment.ProviderStatus)
        {
            return await GetShipmentDtoAsync(shipment.Id, cancellationToken);
        }

        shipment.ProviderStatus = tracking.ProviderStatus;
        shipment.NormalizedStatus = tracking.NormalizedStatus;
        shipment.UpdatedAtUtc = DateTime.UtcNow;

        dbContext.ShipmentTrackingEvents.Add(new ShipmentTrackingEvent
        {
            ShipmentId = shipment.Id,
            ProviderStatus = tracking.ProviderStatus,
            NormalizedStatus = tracking.NormalizedStatus,
            Description = tracking.Description,
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        if (StatusToOrderTransition.TryGetValue(tracking.NormalizedStatus, out var newOrderStatus))
        {
            try
            {
                await orderService.ChangeStatusAsync(
                    shipment.OrderId,
                    new ChangeOrderStatusRequest(newOrderStatus, "Mise à jour automatique du suivi transporteur."),
                    null,
                    cancellationToken);
            }
            catch (ConflictAppException)
            {
                // The order may already be in a state where this transition isn't valid (e.g. an admin
                // already moved it manually) — the tracking event above is still recorded regardless.
            }
        }

        return await GetShipmentDtoAsync(shipment.Id, cancellationToken);
    }

    public async Task<string> GetLabelAsync(Guid shipmentId, CancellationToken cancellationToken = default)
    {
        var shipment = await dbContext.Shipments.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == shipmentId, cancellationToken)
            ?? throw new NotFoundAppException("Expédition introuvable.");

        if (shipment.Carrier != ShippingCarrier.Fake)
        {
            throw new NotConfiguredAppException("Le téléchargement d'étiquette n'est pas implémenté pour ce transporteur.");
        }

        return $"""
            ===== ÉTIQUETTE {FakeShippingProvider.DisplayName} (SIMULATION — usage développement uniquement) =====
            Numéro de suivi : {shipment.TrackingNumber}
            Référence transporteur : {shipment.ProviderShipmentId}
            Statut : {shipment.ProviderStatus}
            ===================================================================================
            """;
    }

    public IReadOnlyList<ShippingCarrierAvailabilityDto> GetCarrierAvailability() =>
        Enum.GetValues<ShippingCarrier>()
            .Select(carrier => _providers.TryGetValue(carrier, out var provider)
                ? new ShippingCarrierAvailabilityDto(
                    carrier,
                    provider.IsConfigured,
                    provider.IsConfigured ? null : "Non implémenté : documentation API officielle du transporteur non disponible.")
                : new ShippingCarrierAvailabilityDto(carrier, false, "Aucun adaptateur enregistré pour ce transporteur."))
            .ToList();

    private async Task<ShipmentDto> GetShipmentDtoAsync(Guid shipmentId, CancellationToken cancellationToken)
    {
        var shipment = await dbContext.Shipments.AsNoTracking()
            .Include(s => s.TrackingEvents)
            .FirstAsync(s => s.Id == shipmentId, cancellationToken);

        return new ShipmentDto(
            shipment.Id,
            shipment.OrderId,
            shipment.Carrier,
            shipment.ProviderShipmentId,
            shipment.TrackingNumber,
            shipment.ProviderStatus,
            shipment.NormalizedStatus,
            shipment.CreatedAtUtc,
            shipment.TrackingEvents
                .OrderBy(e => e.OccurredAtUtc)
                .Select(e => new ShipmentTrackingEventDto(e.Id, e.ProviderStatus, e.NormalizedStatus, e.Description, e.OccurredAtUtc))
                .ToList());
    }

    private IShippingProvider GetProvider(ShippingCarrier carrier) =>
        _providers.TryGetValue(carrier, out var provider)
            ? provider
            : throw new NotConfiguredAppException($"Aucun adaptateur n'est enregistré pour le transporteur {carrier}.");
}
