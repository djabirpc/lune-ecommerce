using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Shipping;
using Ecommerce.Application.Shipping.Dtos;
using Ecommerce.Domain.Orders;
using Ecommerce.Domain.Shipping;
using Ecommerce.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Shipping;

public class ShippingRateService(
    AppDbContext dbContext,
    IValidator<UpdateShippingRateRequest> validator) : IShippingRateService
{
    public async Task<IReadOnlyList<ShippingRateDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.ShippingRates.AsNoTracking()
            .OrderBy(r => r.Wilaya)
            .Select(r => new ShippingRateDto(r.Wilaya, r.HomeDeliveryPrice, r.StopDeskPrice, r.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<ShippingRateDto> UpdateAsync(string wilaya, UpdateShippingRateRequest request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var rate = await dbContext.ShippingRates
            .FirstOrDefaultAsync(r => r.Wilaya == wilaya, cancellationToken)
            ?? throw new NotFoundAppException($"Aucun tarif configuré pour la wilaya \"{wilaya}\".");

        rate.HomeDeliveryPrice = request.HomeDeliveryPrice;
        rate.StopDeskPrice = request.StopDeskPrice;
        rate.IsActive = request.IsActive;
        rate.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return new ShippingRateDto(rate.Wilaya, rate.HomeDeliveryPrice, rate.StopDeskPrice, rate.IsActive);
    }

    public async Task<decimal> GetPriceAsync(string wilaya, DeliveryType deliveryType, CancellationToken cancellationToken = default)
    {
        var rate = await dbContext.ShippingRates.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Wilaya == wilaya, cancellationToken);

        if (rate is null || !rate.IsActive)
        {
            throw new ValidationAppException($"La livraison n'est pas disponible pour la wilaya \"{wilaya}\".");
        }

        return deliveryType == DeliveryType.StopDesk ? rate.StopDeskPrice : rate.HomeDeliveryPrice;
    }
}
