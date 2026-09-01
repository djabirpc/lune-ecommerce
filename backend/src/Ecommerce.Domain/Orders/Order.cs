using Ecommerce.Domain.Common;

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
    public decimal Total { get; set; }

    public ICollection<OrderItem> Items { get; set; } = [];
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = [];
}
