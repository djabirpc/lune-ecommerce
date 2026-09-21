using Ecommerce.Application.Common.Exceptions;
using Ecommerce.Application.Shipping;
using Ecommerce.Application.Shipping.Dtos;
using Ecommerce.Domain.Shipping;
using Ecommerce.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Shipping;

public class CarrierShippingRateService(
    AppDbContext dbContext,
    IValidator<SaveCarrierShippingRateRequest> validator) : ICarrierShippingRateService
{
    public async Task<IReadOnlyList<CarrierShippingRateDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.CarrierShippingRates.AsNoTracking()
            .OrderBy(r => r.Carrier).ThenBy(r => r.Wilaya)
            .Select(r => new CarrierShippingRateDto(r.Id, r.Carrier, r.Wilaya, r.HomeDeliveryPrice, r.StopDeskPrice, r.Notes))
            .ToListAsync(cancellationToken);

    public async Task<CarrierShippingRateDto> CreateAsync(SaveCarrierShippingRateRequest request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsureUniqueAsync(request, existingId: null, cancellationToken);

        var rate = new CarrierShippingRate
        {
            Carrier = request.Carrier,
            Wilaya = request.Wilaya,
            HomeDeliveryPrice = request.HomeDeliveryPrice,
            StopDeskPrice = request.StopDeskPrice,
            Notes = request.Notes,
        };

        dbContext.CarrierShippingRates.Add(rate);
        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(rate);
    }

    public async Task<CarrierShippingRateDto> UpdateAsync(Guid id, SaveCarrierShippingRateRequest request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);
        await EnsureUniqueAsync(request, existingId: id, cancellationToken);

        var rate = await dbContext.CarrierShippingRates.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundAppException("Tarif introuvable.");

        rate.Carrier = request.Carrier;
        rate.Wilaya = request.Wilaya;
        rate.HomeDeliveryPrice = request.HomeDeliveryPrice;
        rate.StopDeskPrice = request.StopDeskPrice;
        rate.Notes = request.Notes;
        rate.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        return ToDto(rate);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rate = await dbContext.CarrierShippingRates.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
            ?? throw new NotFoundAppException("Tarif introuvable.");

        dbContext.CarrierShippingRates.Remove(rate);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureUniqueAsync(SaveCarrierShippingRateRequest request, Guid? existingId, CancellationToken cancellationToken)
    {
        var conflict = await dbContext.CarrierShippingRates.AsNoTracking()
            .AnyAsync(r => r.Carrier == request.Carrier && r.Wilaya == request.Wilaya && r.Id != (existingId ?? Guid.Empty), cancellationToken);

        if (conflict)
        {
            throw new ConflictAppException($"Un tarif existe déjà pour {request.Carrier} / {request.Wilaya}.");
        }
    }

    private static CarrierShippingRateDto ToDto(CarrierShippingRate r) =>
        new(r.Id, r.Carrier, r.Wilaya, r.HomeDeliveryPrice, r.StopDeskPrice, r.Notes);
}
