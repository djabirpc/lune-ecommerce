using Ecommerce.Application.Catalog.Dtos;
using Ecommerce.Application.Catalog.Validators;

namespace Ecommerce.Application.Tests.Catalog;

public class CreateProductRequestValidatorTests
{
    private readonly CreateProductRequestValidator _validator = new();

    private static CreateProductRequest ValidRequest() => new(
        Guid.NewGuid(),
        "Robe longue fleurie",
        "robe-longue-fleurie",
        "Une jolie robe.",
        4500m,
        [new CreateProductVariantRequest("Beige", "S", "ROBE-BEIGE-S", null, 10)]);

    [Fact]
    public async Task ValidRequest_PassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidRequest());

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task NoVariants_FailsValidation()
    {
        var request = ValidRequest() with { Variants = [] };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task DuplicateSkus_FailsValidation()
    {
        var request = ValidRequest() with
        {
            Variants =
            [
                new CreateProductVariantRequest("Beige", "S", "SKU-1", null, 5),
                new CreateProductVariantRequest("Beige", "M", "SKU-1", null, 5),
            ],
        };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("Robe Longue")]
    [InlineData("robe_longue")]
    [InlineData("-robe-longue")]
    public async Task InvalidSlug_FailsValidation(string slug)
    {
        var request = ValidRequest() with { Slug = slug };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ZeroPrice_FailsValidation()
    {
        var request = ValidRequest() with { Price = 0 };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
    }
}
