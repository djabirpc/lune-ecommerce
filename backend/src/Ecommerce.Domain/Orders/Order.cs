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

    // Marketing attribution (CLAUDE.md section 21) — captured client-side at checkout, stored as a
    // flat snapshot on the order (not a separate table) since it's always 1:1, write-once, and never
    // queried independently of an order except for aggregate reporting (see UtmSource index).
    public string? UtmSource { get; set; }
    public string? UtmMedium { get; set; }
    public string? UtmCampaign { get; set; }
    public string? UtmContent { get; set; }
    public string? UtmTerm { get; set; }
    public string? Fbclid { get; set; }
    public string? Ttclid { get; set; }
    public string? Referrer { get; set; }
    public string? LandingPage { get; set; }

    public ICollection<OrderItem> Items { get; set; } = [];
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = [];
    public ICollection<OrderCallAttempt> CallAttempts { get; set; } = [];
    public ICollection<OrderPromotion> AppliedPromotions { get; set; } = [];
    public Shipment? Shipment { get; set; }
}
