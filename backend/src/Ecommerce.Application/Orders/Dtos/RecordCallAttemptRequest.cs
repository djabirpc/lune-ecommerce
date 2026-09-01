using Ecommerce.Domain.Orders;

namespace Ecommerce.Application.Orders.Dtos;

public record RecordCallAttemptRequest(CallAttemptResult Result, string? Notes, DateTime? NextCallAt);
