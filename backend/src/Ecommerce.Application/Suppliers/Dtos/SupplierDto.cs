namespace Ecommerce.Application.Suppliers.Dtos;

public record SupplierDto(
    Guid Id,
    string Name,
    string? Phone,
    string? Email,
    string? Address,
    string? Notes,
    bool IsActive);
