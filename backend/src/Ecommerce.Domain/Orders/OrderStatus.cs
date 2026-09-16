namespace Ecommerce.Domain.Orders;

public enum OrderStatus
{
    PendingConfirmation,
    Confirmed,
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
