namespace Ecommerce.Application.Users.Dtos;

public record CreateUserRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    IReadOnlyList<string> Roles);
