using Ecommerce.Application.Auth.Dtos;
using Ecommerce.Application.Auth.Validators;

namespace Ecommerce.Application.Tests.Auth;

public class LoginRequestValidatorTests
{
    private readonly LoginRequestValidator _validator = new();

    [Fact]
    public async Task ValidRequest_PassesValidation()
    {
        var result = await _validator.ValidateAsync(new LoginRequest("user@luna.dz", "P@ssword1"));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("", "P@ssword1")]
    [InlineData("not-an-email", "P@ssword1")]
    [InlineData("user@luna.dz", "")]
    public async Task InvalidRequest_FailsValidation(string email, string password)
    {
        var result = await _validator.ValidateAsync(new LoginRequest(email, password));

        Assert.False(result.IsValid);
    }
}
