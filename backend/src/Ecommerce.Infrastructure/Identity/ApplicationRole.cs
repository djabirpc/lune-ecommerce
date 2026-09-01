using Microsoft.AspNetCore.Identity;

namespace Ecommerce.Infrastructure.Identity;

public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() { }

    public ApplicationRole(string roleName) : base(roleName) { }
}
