namespace Ecommerce.Application.Users.Dtos;

public record UpdateUserRequest(
    string FirstName,
    string LastName,
    bool IsActive,
    IReadOnlyList<string> Roles);
