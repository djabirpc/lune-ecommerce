using Ecommerce.Application.Auth.Dtos;
using Ecommerce.Application.Auth.Validators;

namespace Ecommerce.Application.Tests.Auth;

public class RefreshTokenRequestValidatorTests
{
    private readonly RefreshTokenRequestValidator _validator = new();

    [Fact]
    public async Task ValidRequest_PassesValidation()
    {
        var result = await _validator.ValidateAsync(new RefreshTokenRequest("some-token-value"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task EmptyToken_FailsValidation()
    {
        var result = await _validator.ValidateAsync(new RefreshTokenRequest(""));

        Assert.False(result.IsValid);
    }
}
