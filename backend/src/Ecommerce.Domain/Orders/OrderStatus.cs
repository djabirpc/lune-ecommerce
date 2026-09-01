namespace Ecommerce.Domain.Orders;

public enum OrderStatus
{
    PendingConfirmation,
    Confirmed,
    Preparing,
    ReadyToShip,
    Shipped,
    OutForDelivery,
    Delivered,
    Cancelled,
    CustomerUnreachable,
    DeliveryFailed,
    Refused,
    Returned,
}
