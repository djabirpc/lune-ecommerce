namespace Ecommerce.Domain.Shipping;

public enum NormalizedShippingStatus
{
    Created,
    PickedUp,
    InTransit,
    AtDestination,
    OutForDelivery,
    Delivered,
    Failed,
    Refused,
    Returned,
    Cancelled,
    Unknown,
}
