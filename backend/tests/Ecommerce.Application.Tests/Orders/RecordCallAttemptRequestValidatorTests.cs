using Ecommerce.Application.Orders.Dtos;
using Ecommerce.Application.Orders.Validators;
using Ecommerce.Domain.Orders;

namespace Ecommerce.Application.Tests.Orders;

public class RecordCallAttemptRequestValidatorTests
{
    private readonly RecordCallAttemptRequestValidator _validator = new();

    [Fact]
    public async Task NoAnswer_WithoutNextCallAt_PassesValidation()
    {
        var request = new RecordCallAttemptRequest(CallAttemptResult.NoAnswer, "Pas de réponse", null);

        var result = await _validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Confirmed_WithoutNextCallAt_PassesValidation()
    {
        var request = new RecordCallAttemptRequest(CallAttemptResult.Confirmed, null, null);

        var result = await _validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task CallbackScheduled_WithoutNextCallAt_FailsValidation()
    {
        var request = new RecordCallAttemptRequest(CallAttemptResult.CallbackScheduled, null, null);

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task CallbackScheduled_WithPastNextCallAt_FailsValidation()
    {
        var request = new RecordCallAttemptRequest(CallAttemptResult.CallbackScheduled, null, DateTime.UtcNow.AddDays(-1));

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task CallbackScheduled_WithFutureNextCallAt_PassesValidation()
    {
        var request = new RecordCallAttemptRequest(CallAttemptResult.CallbackScheduled, "Rappeler demain", DateTime.UtcNow.AddDays(1));

        var result = await _validator.ValidateAsync(request);

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task NotesTooLong_FailsValidation()
    {
        var request = new RecordCallAttemptRequest(CallAttemptResult.NoAnswer, new string('a', 1001), null);

        var result = await _validator.ValidateAsync(request);

        Assert.False(result.IsValid);
    }
}
