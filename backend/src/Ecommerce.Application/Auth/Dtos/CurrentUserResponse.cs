namespace Ecommerce.Application.Auth.Dtos;

public record CurrentUserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyList<string> Roles);
