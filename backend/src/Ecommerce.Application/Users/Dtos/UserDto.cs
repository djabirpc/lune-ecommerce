namespace Ecommerce.Application.Users.Dtos;

public record UserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    bool IsActive,
    IReadOnlyList<string> Roles,
    DateTime CreatedAtUtc);
