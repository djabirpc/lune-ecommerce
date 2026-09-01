using Ecommerce.Application.Inventory.Dtos;
using Ecommerce.Application.Inventory.Validators;

namespace Ecommerce.Application.Tests.Inventory;

public class AdjustInventoryRequestValidatorTests
{
    private readonly AdjustInventoryRequestValidator _validator = new();

    [Fact]
    public async Task ValidRequest_PassesValidation()
    {
        var result = await _validator.ValidateAsync(new AdjustInventoryRequest(Guid.NewGuid(), -2, "Casse en entrepôt"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task ZeroDelta_FailsValidation()
    {
        var result = await _validator.ValidateAsync(new AdjustInventoryRequest(Guid.NewGuid(), 0, "Raison"));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task EmptyReason_FailsValidation()
    {
        var result = await _validator.ValidateAsync(new AdjustInventoryRequest(Guid.NewGuid(), 5, ""));

        Assert.False(result.IsValid);
    }
}
