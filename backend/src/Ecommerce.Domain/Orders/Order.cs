using Ecommerce.Domain.Common;
using Ecommerce.Domain.Shipping;

namespace Ecommerce.Domain.Orders;

public class Order : Entity
{
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.PendingConfirmation;

    /// <summary>Null for a normal guest/customer checkout. Set when a staff member creates the order
    /// on the customer's behalf (e.g. a phone call: "I want this dress in this size") via the admin
    /// panel — those orders start life already Confirmed, since the call itself is the confirmation.</summary>
    public Guid? CreatedByUserId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Wilaya { get; set; } = string.Empty;
    public string Commune { get; set; } = string.Empty;

    /// <summary>Optional — a Stop Desk pickup doesn't need a street address, and some Home Delivery
    /// customers still just give the driver a phone-call description on arrival.</summary>
    public string? Address { get; set; }

    public DeliveryType DeliveryType { get; set; }
    public string? Notes { get; set; }

    public string PaymentMethod { get; set; } = "COD";
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    /// <summary>Set only when Status becomes Returned — the structured cause (damaged, wrong size, ...) used for admin reporting.</summary>
    public OrderReturnReason? ReturnReason { get; set; }

    public decimal Subtotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal DiscountTotal { get; set; }
    public decimal Total { get; set; }

    /// <summary>The coupon code applied at creation, if any — stored so it can be reapplied every
    /// time totals are recalculated (e.g. an item is added/removed later), not just once at creation.</summary>
    public string? CouponCode { get; set; }

    /// <summary>Phone-negotiated fixed DA discount, on top of any automatic promotions/coupon. Persisted
    /// (not just a one-time creation parameter) so it survives later edits — see RecalculateTotalsAsync.</summary>
    public decimal? ManualDiscountAmount { get; set; }

    /// <summary>Phone-negotiated free shipping, independent of any FreeShipping promotion. Same
    /// persistence rationale as ManualDiscountAmount.</summary>
    public bool NegotiatedFreeShipping { get; set; }

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
