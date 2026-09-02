namespace Ecommerce.Application.Shipping;

/// <summary>What a shipping provider needs to create a shipment — deliberately minimal, not a full Order DTO.</summary>
public record ShipmentRequest(
    string OrderNumber,
    string RecipientFirstName,
    string RecipientLastName,
    string Phone,
    string Wilaya,
    string Commune,
    string Address,
    decimal CodAmount);
