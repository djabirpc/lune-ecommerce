namespace Ecommerce.Application.Orders.Dtos;

/// <summary>Sets (or clears) a phone-negotiated discount/free-shipping on an existing order —
/// the same mechanism CreateAdminOrderRequest offers at creation, made editable afterward too
/// (e.g. the customer adds items mid-call and the agent offers free shipping for the trouble).</summary>
public record UpdateOrderNegotiationRequest(decimal? ManualDiscountAmount, bool FreeShipping);
