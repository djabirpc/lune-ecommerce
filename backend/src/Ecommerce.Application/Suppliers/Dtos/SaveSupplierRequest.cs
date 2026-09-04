namespace Ecommerce.Application.Suppliers.Dtos;

public record SaveSupplierRequest(
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? Notes,
    bool IsActive);
