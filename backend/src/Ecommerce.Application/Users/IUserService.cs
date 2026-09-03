using Ecommerce.Application.Common;
using Ecommerce.Application.Users.Dtos;

namespace Ecommerce.Application.Users;

public interface IUserService
{
    Task<PagedResult<UserDto>> GetPagedAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<UserDto> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default);

    /// <summary>currentUserId is used to reject an admin deactivating their own account.</summary>
    Task<UserDto> UpdateAsync(Guid id, UpdateUserRequest request, Guid currentUserId, CancellationToken cancellationToken = default);
}
