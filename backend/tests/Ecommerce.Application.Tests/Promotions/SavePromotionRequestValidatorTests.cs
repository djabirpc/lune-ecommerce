using Ecommerce.Application.Promotions.Dtos;
using Ecommerce.Application.Promotions.Validators;
using Ecommerce.Domain.Promotions;

namespace Ecommerce.Application.Tests.Promotions;

public class SavePromotionRequestValidatorTests
{
    private readonly SavePromotionRequestValidator _validator = new();

    private static SavePromotionRequest ValidPercentageRequest() => new(
        "Soldes d'été",
        "Jusqu'à -20%",
        PromotionType.PercentageDiscount,
        20m,
        null,
        null,
        null,
        null,
        DateTime.UtcNow,
        DateTime.UtcNow.AddDays(7),
        true,
        0,
        [],
        []);

    [Fact]
    public async Task ValidPercentageRequest_PassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidPercentageRequest());

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task PercentageDiscount_WithoutPercentageValue_FailsValidation()
    {
        var request = ValidPercentageRequest() with { PercentageValue = null };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(101)]
    public async Task PercentageDiscount_OutOfRange_FailsValidation(decimal value)
    {
        var request = ValidPercentageRequest() with { PercentageValue = value };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task EndsBeforeStarts_FailsValidation()
    {
        var request = ValidPercentageRequest() with { StartsAtUtc = DateTime.UtcNow, EndsAtUtc = DateTime.UtcNow.AddDays(-1) };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task FixedAmountDiscount_WithoutFixedAmountValue_FailsValidation()
    {
        var request = ValidPercentageRequest() with { Type = PromotionType.FixedAmountDiscount, PercentageValue = null, FixedAmountValue = null };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task FixedAmountDiscount_WithFixedAmountValue_PassesValidation()
    {
        var request = ValidPercentageRequest() with { Type = PromotionType.FixedAmountDiscount, PercentageValue = null, FixedAmountValue = 500m };

        var result = await _validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task BuyXGetY_WithoutQuantities_FailsValidation()
    {
        var request = ValidPercentageRequest() with { Type = PromotionType.BuyXGetY, PercentageValue = null };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task BuyXGetY_WithQuantities_PassesValidation()
    {
        var request = ValidPercentageRequest() with { Type = PromotionType.BuyXGetY, PercentageValue = null, BuyQuantity = 2, GetQuantity = 1 };

        var result = await _validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Coupon_WithoutCode_FailsValidation()
    {
        var request = ValidPercentageRequest() with { Type = PromotionType.Coupon };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Coupon_WithCodeAndPercentage_PassesValidation()
    {
        var request = ValidPercentageRequest() with { Type = PromotionType.Coupon, CouponCode = "SUMMER20" };

        var result = await _validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Coupon_WithBothPercentageAndFixedAmount_FailsValidation()
    {
        var request = ValidPercentageRequest() with { Type = PromotionType.Coupon, CouponCode = "SUMMER20", FixedAmountValue = 100m };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ProductDiscount_WithoutProductIds_FailsValidation()
    {
        var request = ValidPercentageRequest() with { Type = PromotionType.ProductDiscount };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ProductDiscount_WithProductIds_PassesValidation()
    {
        var request = ValidPercentageRequest() with { Type = PromotionType.ProductDiscount, ProductIds = [Guid.NewGuid()] };

        var result = await _validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }
}
