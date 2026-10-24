namespace Ecommerce.Application.Customers.Dtos;

/// <summary>
/// There is no Customer entity (CLAUDE.md's guest-checkout model, Important Decision #18) — every
/// profile here is reconstructed by grouping Orders by Phone, the one identifier guaranteed on
/// every order regardless of guest vs. admin-created. FullName/Wilaya reflect the most recent order.
/// </summary>
public record CustomerSummaryDto(
    string Phone,
    string FullName,
    string Wilaya,
    int TotalOrders,
    int DeliveredOrders,
    int CancelledOrRefusedOrders,
    int UnreachableCount,
    decimal TotalSpent,
    DateTime FirstOrderAtUtc,
    DateTime LastOrderAtUtc);
