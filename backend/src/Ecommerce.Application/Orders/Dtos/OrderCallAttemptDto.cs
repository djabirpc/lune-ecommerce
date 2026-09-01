using Ecommerce.Domain.Orders;

namespace Ecommerce.Application.Orders.Dtos;

public record OrderCallAttemptDto(
    Guid Id,
    int AttemptNumber,
    CallAttemptResult Result,
    string? Notes,
    DateTime CalledAtUtc,
    DateTime? NextCallAtUtc);
