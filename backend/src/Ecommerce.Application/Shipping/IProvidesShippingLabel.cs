namespace Ecommerce.Application.Shipping;

/// <summary>
/// Optional capability implemented only by carriers that expose a real label/bordereau endpoint
/// (currently just Ecotrack48h). Kept separate from <see cref="IShippingProvider"/> since a real PDF
/// label isn't something every carrier's documented API can do — CreateShipmentAsync/GetTrackingAsync
/// are the two universally-required operations (CLAUDE.md section 15), a real label download is not.
/// </summary>
public interface IProvidesShippingLabel
{
    Task<byte[]> GetLabelPdfAsync(string providerShipmentId, CancellationToken cancellationToken = default);
}
