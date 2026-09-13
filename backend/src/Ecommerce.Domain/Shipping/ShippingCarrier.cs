namespace Ecommerce.Domain.Shipping;

public enum ShippingCarrier
{
    Fake,
    Yalidine,
    ZRExpress,

    /// <summary>48h Express — built on the white-label "Ecotrack" delivery platform (shared by
    /// several Algerian carriers). Unlike Yalidine/ZRExpress, real API documentation exists for
    /// this one (see Ecotrack48hShippingProvider), so it's a genuine, functional integration.</summary>
    Ecotrack48h,
}
