namespace Ecommerce.Application.Marketing.Dtos;

/// <summary>Flat snapshot handed to IPurchaseConversionEventSender — deliberately not the full Order
/// entity, so the sender (and the Meta/TikTok HTTP clients behind it) only ever see exactly the fields
/// a "Purchase" conversion event needs.</summary>
public record PurchaseEventData(
    Guid OrderId,
    string OrderNumber,
    decimal Total,
    string Phone,
    DateTime EventTimeUtc);
