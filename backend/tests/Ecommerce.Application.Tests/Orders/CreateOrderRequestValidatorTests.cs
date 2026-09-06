using Ecommerce.Application.Orders.Dtos;
using Ecommerce.Application.Orders.Validators;
using Ecommerce.Domain.Orders;

namespace Ecommerce.Application.Tests.Orders;

public class CreateOrderRequestValidatorTests
{
    private readonly CreateOrderRequestValidator _validator = new();

    private static CreateOrderRequest ValidRequest() => new(
        "Amina",
        "Benali",
        "0551234567",
        "Alger",
        "Bab Ezzouar",
        "12 rue des Frères",
        DeliveryType.HomeDelivery,
        null,
        [new OrderItemRequest(Guid.NewGuid(), 2)]);

    [Fact]
    public async Task ValidRequest_PassesValidation()
    {
        var result = await _validator.ValidateAsync(ValidRequest());

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("0551234567", true)]
    [InlineData("055123456", false)]
    [InlineData("1551234567", false)]
    [InlineData("not-a-phone", false)]
    [InlineData("", false)]
    public async Task PhoneValidation(string phone, bool expectedValid)
    {
        var request = ValidRequest() with { Phone = phone };

        var result = await _validator.ValidateAsync(request);

        Assert.Equal(expectedValid, result.IsValid);
    }

    [Fact]
    public async Task NoItems_FailsValidation()
    {
        var request = ValidRequest() with { Items = [] };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task DuplicateVariantInItems_FailsValidation()
    {
        var variantId = Guid.NewGuid();
        var request = ValidRequest() with
        {
            Items = [new OrderItemRequest(variantId, 1), new OrderItemRequest(variantId, 2)],
        };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task ZeroQuantity_FailsValidation()
    {
        var request = ValidRequest() with { Items = [new OrderItemRequest(Guid.NewGuid(), 0)] };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BlankOrWhitespaceAddress_FailsValidation_WithFrenchMessage(string address)
    {
        // Regression test: FluentValidation's bare NotEmpty()/MaximumLength() without .WithMessage()
        // leaked default English messages (e.g. "'Address' must not be empty.") to guest checkout —
        // a real bug reported by the user, violating CLAUDE.md section 45 (customer-facing UI must
        // be French). Also confirms whitespace-only input (which the frontend's un-trimmed zod
        // schema let through) is correctly rejected server-side, not just truly-empty input.
        var request = ValidRequest() with { Address = address };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "L'adresse de livraison est requise.");
    }

    [Fact]
    public async Task BlankFirstName_FailsValidation_WithFrenchMessage()
    {
        var request = ValidRequest() with { FirstName = "" };

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage == "Le prénom est requis.");
    }
}
