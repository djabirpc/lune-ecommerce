using Ecommerce.Application.Orders.Dtos;
using Ecommerce.Application.Orders.Validators;
using Ecommerce.Domain.Orders;

namespace Ecommerce.Application.Tests.Orders;

public class CreateAdminOrderRequestValidatorTests
{
    private readonly CreateAdminOrderRequestValidator _validator = new();

    private static CreateAdminOrderRequest ValidRequest() => new(
        "Amina",
        "Benali",
        "0551234567",
        "Alger",
        "Bab Ezzouar",
        "12 rue des Frères",
        DeliveryType.HomeDelivery,
        null,
        [new OrderItemRequest(Guid.NewGuid(), 2)],
        null,
        null,
        false);

    [Fact]
    public async Task ValidRequest_PassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidRequest());

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task PositiveManualDiscount_PassesValidation()
    {
        var request = ValidRequest() with { ManualDiscountAmount = 500m };

        var result = await _validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task NegativeManualDiscount_FailsValidation()
    {
        var request = ValidRequest() with { ManualDiscountAmount = -100m };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task NoItems_FailsValidation()
    {
        var request = ValidRequest() with { Items = [] };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
    }
}
