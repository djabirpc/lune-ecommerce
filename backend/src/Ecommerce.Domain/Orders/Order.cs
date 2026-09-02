using Ecommerce.Domain.Common;
using Ecommerce.Domain.Shipping;

namespace Ecommerce.Domain.Orders;

public class Order : Entity
{
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.PendingConfirmation;

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Wilaya { get; set; } = string.Empty;
    public string Commune { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DeliveryType DeliveryType { get; set; }
    public string? Notes { get; set; }

    public string PaymentMethod { get; set; } = "COD";
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    public decimal Subtotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal Total { get; set; }

    public ICollection<OrderItem> Items { get; set; } = [];
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = [];
    public ICollection<OrderCallAttempt> CallAttempts { get; set; } = [];
    public ICollection<OrderPromotion> AppliedPromotions { get; set; } = [];
    public Shipment? Shipment { get; set; }
}
