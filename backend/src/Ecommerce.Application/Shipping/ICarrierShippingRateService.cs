using Ecommerce.Application.Shipping.Dtos;

namespace Ecommerce.Application.Shipping;

/// <summary>Admin-entered reference rates per carrier/wilaya — see CarrierShippingRate's doc comment
/// for why this never touches real checkout pricing.</summary>
public interface ICarrierShippingRateService
{
    Task<IReadOnlyList<CarrierShippingRateDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<CarrierShippingRateDto> CreateAsync(SaveCarrierShippingRateRequest request, CancellationToken cancellationToken = default);

    Task<CarrierShippingRateDto> UpdateAsync(Guid id, SaveCarrierShippingRateRequest request, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
