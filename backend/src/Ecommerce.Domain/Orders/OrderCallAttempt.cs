using Ecommerce.Domain.Common;

namespace Ecommerce.Domain.Orders;

public class OrderCallAttempt : Entity
{
    public Guid OrderId { get; set; }
    public Guid AgentUserId { get; set; }
    public int AttemptNumber { get; set; }
    public CallAttemptResult Result { get; set; }
    public string? Notes { get; set; }
    public DateTime CalledAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? NextCallAtUtc { get; set; }

    public Order Order { get; set; } = null!;
}
